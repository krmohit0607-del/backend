using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.Features.Settings.Commands;
using MultiTenantSaaS.Domain.Entities;
using System.Globalization;
using System.Text.Json;

namespace MultiTenantSaaS.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SettingsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<SettingResponse>> Get(string key, CancellationToken ct)
    {
        var setting = await _db.UserSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        return setting is null ? NotFound() : Ok(new SettingResponse(setting.Key, setting.ValueJson, setting.UpdatedAt));
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<SettingResponse>> Put(string key, [FromBody] SettingRequest request, CancellationToken ct)
    {
        var setting = await _db.UserSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (setting is null)
        {
            setting = new UserSetting { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, Key = key };
            _db.UserSettings.Add(setting);
        }

        setting.ValueJson = request.ValueJson;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedByUserId = _currentUser.UserId;

        // Voyage recaps (Operations/Postfix) additionally sync their Hire & Claims data into
        // structured tables, so fleet-wide reporting can query real rows instead of parsing JSON.
        if (key.StartsWith("opsRecap.", StringComparison.Ordinal))
        {
            var voyageId = key["opsRecap.".Length..];
            if (!string.IsNullOrWhiteSpace(voyageId))
            {
                try { await SyncHireAndClaimsAsync(voyageId, request.ValueJson, ct); }
                catch (JsonException) { /* malformed payload — skip structured sync, settings save still succeeds */ }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new SettingResponse(setting.Key, setting.ValueJson, setting.UpdatedAt));
    }

    // -------------------------------------------------------------- Hire & Claims sync

    private static bool IsHireLocked(string? status) => status is "Sent For Payment" or "Paid & Locked";

    private static DateTime? ParseFlexibleDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s) || s == "—") return null;
        string[] formats = { "dd-MM-yyyy HH:mm", "dd-MM-yyyy" };
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose) ? loose : null;
    }

    private async Task SyncHireAndClaimsAsync(string voyageId, string valueJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(valueJson);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return;

        var vesselName = root.TryGetProperty("vesselName", out var vn) && vn.ValueKind == JsonValueKind.String ? vn.GetString() : null;
        var hireCurrency = root.TryGetProperty("hireCurrency", out var hc) && hc.ValueKind == JsonValueKind.String ? hc.GetString() : "USD";
        var charterHireCurrency = root.TryGetProperty("charterHireCurrency", out var chc) && chc.ValueKind == JsonValueKind.String ? chc.GetString() : "USD";

        await SyncClaimsAsync(voyageId, vesselName, root, ct);
        await SyncHireScheduleAsync(voyageId, vesselName, root, "Owners", "hireScheduleSnapshot", hireCurrency, ct);
        await SyncHireScheduleAsync(voyageId, vesselName, root, "Charterers", "charterHireScheduleSnapshot", charterHireCurrency, ct);
        await SyncHireDuplicatesAsync(voyageId, vesselName, root, "Owners", "hireDuplicates", hireCurrency, ct);
        await SyncHireDuplicatesAsync(voyageId, vesselName, root, "Charterers", "charterHireDuplicates", charterHireCurrency, ct);
    }

    private async Task SyncClaimsAsync(string voyageId, string? vesselName, JsonElement root, CancellationToken ct)
    {
        var incoming = new List<ClaimSyncDto>();
        if (root.TryGetProperty("freightLaytime", out var fl) && fl.ValueKind == JsonValueKind.Object
            && fl.TryGetProperty("settlement", out var settlement) && settlement.ValueKind == JsonValueKind.Object
            && settlement.TryGetProperty("claims", out var claimsEl) && claimsEl.ValueKind == JsonValueKind.Array)
        {
            incoming = JsonSerializer.Deserialize<List<ClaimSyncDto>>(claimsEl.GetRawText(), JsonOpts) ?? new();
        }

        var existing = await _db.ClaimRecords.Where(c => c.VoyageId == voyageId).ToListAsync(ct);
        var incomingKeys = new HashSet<string>(incoming.Where(c => !string.IsNullOrEmpty(c.Id)).Select(c => c.Id!));

        foreach (var stale in existing.Where(e => !incomingKeys.Contains(e.ClaimKey)))
        {
            if (IsHireLocked(stale.WorkflowStatus)) continue; // never silently drop a locked/paid claim
            _db.ClaimRecords.Remove(stale);
        }

        foreach (var c in incoming)
        {
            if (string.IsNullOrEmpty(c.Id)) continue;
            var row = existing.FirstOrDefault(e => e.ClaimKey == c.Id);
            if (row != null && IsHireLocked(row.WorkflowStatus) && IsHireLocked(c.WorkflowStatus))
                continue; // already locked and still locked — immutable, skip

            if (row is null)
            {
                row = new ClaimRecord { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, VoyageId = voyageId, ClaimKey = c.Id };
                _db.ClaimRecords.Add(row);
            }

            row.VesselName = vesselName;
            row.ClaimType = c.Type ?? string.Empty;
            row.ClaimReference = c.Reference ?? string.Empty;
            row.Owner = c.ChargeTo ?? c.Owner;
            row.Currency = c.Currency ?? "USD";
            row.Amount = c.Amount;
            row.Settlement = c.Settlement;
            row.Status = c.Status ?? "Open";
            row.PaymentStatus = c.PaymentStatus;
            row.WorkflowStatus = c.WorkflowStatus;
            row.DueDate = ParseFlexibleDate(c.Due);
            row.AttachmentsJson = c.Attachments?.ValueKind is JsonValueKind.Array or JsonValueKind.Object ? c.Attachments.Value.GetRawText() : null;
        }
    }

    private async Task SyncHireScheduleAsync(string voyageId, string? vesselName, JsonElement root, string side, string propName, string? currency, CancellationToken ct)
    {
        var incoming = new List<HireScheduleRowDto>();
        if (root.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.Array)
            incoming = JsonSerializer.Deserialize<List<HireScheduleRowDto>>(el.GetRawText(), JsonOpts) ?? new();

        var existing = await _db.HirePayments.Where(h => h.VoyageId == voyageId && h.Side == side && !h.IsDuplicate).ToListAsync(ct);
        var incomingKeys = new HashSet<string>(incoming.Where(r => !string.IsNullOrEmpty(r.Key)).Select(r => r.Key!));

        foreach (var stale in existing.Where(e => !incomingKeys.Contains(e.InstallmentKey)))
        {
            if (IsHireLocked(stale.Status)) continue; // never silently drop a locked/paid installment
            _db.HirePayments.Remove(stale);
        }

        foreach (var r in incoming)
        {
            if (string.IsNullOrEmpty(r.Key) || r.Deleted) continue;
            var row = existing.FirstOrDefault(e => e.InstallmentKey == r.Key);
            if (row != null && IsHireLocked(row.Status) && IsHireLocked(r.Status))
                continue; // frozen once locked — ignore further recalculated values

            if (row is null)
            {
                row = new HirePayment { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, VoyageId = voyageId, Side = side, InstallmentKey = r.Key, IsDuplicate = false };
                _db.HirePayments.Add(row);
            }

            row.VesselName = vesselName;
            row.Name = r.Name ?? row.InstallmentKey;
            row.Account = r.Account ?? string.Empty;
            row.FromDate = ParseFlexibleDate(r.From);
            row.ToDate = ParseFlexibleDate(r.To);
            row.DueDate = ParseFlexibleDate(r.Due);
            row.OnHireDays = r.OnHire;
            row.OffHireDays = r.OffHire;
            row.Amount = r.Amount;
            row.Currency = currency ?? "USD";
            row.Status = r.Status ?? "Draft";
            row.Ballast = r.Ballast;
            row.Bunkers = r.Bunkers;
            row.BunkerCredit = r.BunkerCredit;
        }
    }

    private async Task SyncHireDuplicatesAsync(string voyageId, string? vesselName, JsonElement root, string side, string propName, string? currency, CancellationToken ct)
    {
        var incoming = new List<HireDuplicateDto>();
        if (root.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.Array)
            incoming = JsonSerializer.Deserialize<List<HireDuplicateDto>>(el.GetRawText(), JsonOpts) ?? new();

        var existing = await _db.HirePayments.Where(h => h.VoyageId == voyageId && h.Side == side && h.IsDuplicate).ToListAsync(ct);
        var incomingKeys = new HashSet<string>(incoming.Where(r => !string.IsNullOrEmpty(r.Id)).Select(r => r.Id!));

        foreach (var stale in existing.Where(e => !incomingKeys.Contains(e.InstallmentKey)))
        {
            if (IsHireLocked(stale.Status)) continue;
            _db.HirePayments.Remove(stale);
        }

        foreach (var r in incoming)
        {
            if (string.IsNullOrEmpty(r.Id)) continue;
            var row = existing.FirstOrDefault(e => e.InstallmentKey == r.Id);
            if (row != null && IsHireLocked(row.Status) && IsHireLocked(r.Status))
                continue;

            if (row is null)
            {
                row = new HirePayment { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, VoyageId = voyageId, Side = side, InstallmentKey = r.Id, IsDuplicate = true };
                _db.HirePayments.Add(row);
            }

            row.VesselName = vesselName;
            row.Name = r.Name ?? row.InstallmentKey;
            row.Account = r.Account ?? string.Empty;
            row.FromDate = ParseFlexibleDate(r.From);
            row.ToDate = ParseFlexibleDate(r.To);
            row.DueDate = ParseFlexibleDate(r.Due);
            row.OnHireDays = r.OnHire;
            row.OffHireDays = r.OffHire;
            row.Amount = r.Amount;
            row.Currency = currency ?? "USD";
            row.Status = r.Status ?? "Draft";
        }
    }

    private sealed class ClaimSyncDto
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Reference { get; set; }
        public string? ChargeTo { get; set; }
        public string? Owner { get; set; }
        public string? Due { get; set; }
        public string? Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal Settlement { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? WorkflowStatus { get; set; }
        public JsonElement? Attachments { get; set; }
    }

    private sealed class HireScheduleRowDto
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Account { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public decimal OnHire { get; set; }
        public decimal OffHire { get; set; }
        public decimal Amount { get; set; }
        public string? Due { get; set; }
        public string? Status { get; set; }
        public bool Ballast { get; set; }
        public decimal Bunkers { get; set; }
        public decimal BunkerCredit { get; set; }
        public bool Deleted { get; set; }
    }

    private sealed class HireDuplicateDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Account { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public decimal OnHire { get; set; }
        public decimal OffHire { get; set; }
        public decimal Amount { get; set; }
        public string? Due { get; set; }
        public string? Status { get; set; }
    }


    [HttpPost("master-data/imo-ships")]
    public async Task<ActionResult<ApiResponse<int>>> SaveImoShips([FromBody] SaveImoShipsRequest request, CancellationToken ct)
    {
        try
        {
            if (request?.Ships == null || request.Ships.Count == 0)
                return BadRequest(new ApiResponse<int> { Success = false, Message = "No ships to save" });

            var tenantId = _currentUser.TenantId;
            var imoSet = new HashSet<string>(request.Ships.Select(s => s.Imo ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)));

            // Delete existing IMO ships for this tenant
            var existingImos = await _db.ImoShips.Where(i => i.TenantId == tenantId).ToListAsync(ct);
            _db.ImoShips.RemoveRange(existingImos);

            // Add new ones
            var shipEntitiesToAdd = new List<ImoShip>();
            foreach (var shipDto in request.Ships)
            {
                if (string.IsNullOrEmpty(shipDto.Imo) || string.IsNullOrEmpty(shipDto.Name))
                    continue;

                var ship = new ImoShip
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Imo = shipDto.Imo,
                    Name = shipDto.Name,
                    VesselType = shipDto.VesselType,
                    Statcode5 = shipDto.Statcode5,
                    Statcode5Desc = shipDto.Statcode5Desc,
                    BuilderName = shipDto.BuilderName,
                    BuilderCountry = shipDto.BuilderCountry,
                    BuiltYear = shipDto.BuiltYear,
                    Gt = shipDto.Gt,
                    LengthBp = shipDto.LengthBp,
                    LengthOverall = shipDto.LengthOverall,
                    Depth = shipDto.Depth,
                    BreadthMoulded = shipDto.BreadthMoulded,
                    Deadweight = shipDto.Deadweight,
                    Displacement = shipDto.Displacement,
                    Draught = shipDto.Draught,
                    HullType = shipDto.HullType,
                    Holds = shipDto.Holds,
                    Teu = shipDto.Teu,
                    GasCapacity = shipDto.GasCapacity,
                    EngineBuilder = shipDto.EngineBuilder,
                    EngineDesign = shipDto.EngineDesign,
                    EngineModel = shipDto.EngineModel,
                    EnginesRpm = shipDto.EnginesRpm,
                    TotalKwMainEng = shipDto.TotalKwMainEng,
                    FuelConsMainEng = shipDto.FuelConsMainEng,
                    AuxEngineTotalKw = shipDto.AuxEngineTotalKw,
                    GeneratorsKw = shipDto.GeneratorsKw,
                    ClassSociety = shipDto.ClassSociety,
                    Flag = shipDto.Flag,
                    Owner = shipDto.Owner,
                    Operator = shipDto.Operator,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByUserId = _currentUser.UserId,
                    UpdatedByUserId = _currentUser.UserId
                };
                shipEntitiesToAdd.Add(ship);
            }

            _db.ImoShips.AddRange(shipEntitiesToAdd);
            await _db.SaveChangesAsync(ct);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = $"Saved {shipEntitiesToAdd.Count} IMO ships",
                Data = shipEntitiesToAdd.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<int> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("master-data/ports")]
    public async Task<ActionResult<ApiResponse<int>>> SavePorts([FromBody] SavePortsRequest request, CancellationToken ct)
    {
        try
        {
            if (request?.Ports == null || request.Ports.Count == 0)
                return BadRequest(new ApiResponse<int> { Success = false, Message = "No ports to save" });

            var tenantId = _currentUser.TenantId;

            // Delete existing ports for this tenant
            var existingPorts = await _db.Ports.Where(p => p.TenantId == tenantId).ToListAsync(ct);
            _db.Ports.RemoveRange(existingPorts);

            // Add new ones
            var portEntitiesToAdd = new List<Port>();
            foreach (var portDto in request.Ports)
            {
                if (string.IsNullOrEmpty(portDto.PortName) || string.IsNullOrEmpty(portDto.PortCode))
                    continue;

                var port = new Port
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PortName = portDto.PortName,
                    PortCode = portDto.PortCode,
                    UnLocode = portDto.UnLocode,
                    Country = portDto.Country ?? string.Empty,
                    Region = portDto.Region,
                    Latitude = portDto.Latitude,
                    Longitude = portDto.Longitude,
                    PortType = portDto.PortType,
                    IsRiver = portDto.IsRiver,
                    IsCanalEntrance = portDto.IsCanalEntrance,
                    Facilities = portDto.Facilities,
                    Remarks = portDto.Remarks,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByUserId = _currentUser.UserId,
                    UpdatedByUserId = _currentUser.UserId
                };
                portEntitiesToAdd.Add(port);
            }

            _db.Ports.AddRange(portEntitiesToAdd);
            await _db.SaveChangesAsync(ct);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = $"Saved {portEntitiesToAdd.Count} ports",
                Data = portEntitiesToAdd.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<int> { Success = false, Message = ex.Message });
        }
    }
}

public record SettingRequest(string ValueJson);
public record SettingResponse(string Key, string ValueJson, DateTime UpdatedAt);
