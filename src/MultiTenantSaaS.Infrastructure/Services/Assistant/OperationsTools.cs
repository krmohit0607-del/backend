using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

internal sealed class GetOperationsRecapTool(IApplicationDbContext db) : IAssistantTool
{
    private const int MaxSectionCharacters = 15_000;

    public string Name => "get_operations_recap";
    public string Description => "Read a voyage's Operations recap: charter-party terms, hire, freight, laytime, ETA/ROB itinerary, bunkers and claims as entered in Operations. Without 'section' it returns all simple fields plus the names of the detailed sections; ask again with a section name for the detail.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        ToolArgs.VoyageParameter,
        new("section", "string", "Name of a detailed section returned by an earlier call, e.g. 'etaPlan'.")
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();

        var keys = voyage.Keys.Select(k => $"opsRecap.{k}").ToArray();
        var setting = await db.UserSettings.AsNoTracking().Where(s => keys.Contains(s.Key)).OrderByDescending(s => s.UpdatedAt).FirstOrDefaultAsync(ct);
        if (setting is null) return new { voyage = voyage.Display, error = "No Operations recap has been saved for this voyage." };

        if (JsonNode.Parse(setting.ValueJson) is not JsonObject recap) return new { error = "The stored recap could not be read." };
        AssistantContextSanitizer.SanitizeNode(recap);

        var section = ToolArgs.Text(args, "section");
        if (section.Length > 0)
        {
            var match = recap.FirstOrDefault(p => string.Equals(p.Key, section, StringComparison.OrdinalIgnoreCase));
            if (match.Key is null) return new { error = $"No section named '{section}'.", sections = recap.Where(p => p.Value is JsonObject or JsonArray).Select(p => p.Key) };
            var json = match.Value?.ToJsonString() ?? "null";
            if (json.Length > MaxSectionCharacters)
                return new { voyage = voyage.Display, section = match.Key, truncated = true, data = json[..MaxSectionCharacters] };
            return new { voyage = voyage.Display, section = match.Key, truncated = false, data = (object?)match.Value };
        }

        var fields = new JsonObject();
        foreach (var pair in recap.Where(p => p.Value is JsonValue)) fields[pair.Key] = pair.Value?.DeepClone();
        return new
        {
            voyage = voyage.Display,
            savedAtUtc = setting.UpdatedAt,
            fields,
            sections = recap.Where(p => p.Value is JsonObject or JsonArray).Select(p => p.Key).ToList()
        };
    }
}

internal sealed class ListBunkerRequirementsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_bunker_requirements";
    public string Description => "List bunker requirements with status, port, ETA, fuel, quantity, ROB on arrival, supplier, price, invoice and payment state. Filter by vessel, status, port or requirement number.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("requirementNo", "string", "Exact requirement number, e.g. BR-2606-024."),
        new("vessel", "string", "Part of the vessel name."),
        new("status", "string", "Part of the status, e.g. 'Booked'."),
        new("port", "string", "Part of the bunker port name."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.BunkerRequirements.AsNoTracking().AsQueryable();
        var no = ToolArgs.Text(args, "requirementNo");
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        var port = ToolArgs.Text(args, "port").ToLowerInvariant();
        if (no.Length > 0) query = query.Where(b => b.RequirementNo == no);
        if (vessel.Length > 0) query = query.Where(b => b.VesselName.ToLower().Contains(vessel));
        if (status.Length > 0) query = query.Where(b => b.Status.ToLower().Contains(status));
        if (port.Length > 0) query = query.Where(b => b.BunkerPort.ToLower().Contains(port));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(b => b.CreatedAt).Take(ToolArgs.Limit(args)).Select(b => new
        {
            requirementNo = b.RequirementNo,
            status = b.Status,
            priority = b.Priority,
            vessel = b.VesselName,
            route = b.Route,
            bunkerPort = b.BunkerPort,
            eta = b.Eta,
            requiredOn = b.RequiredOn,
            fuelType = b.FuelType,
            quantityMt = b.Quantity,
            robOnArrivalMt = b.RobArrival,
            expectedConsMt = b.ExpectedCons,
            supplier = b.Supplier,
            pricePerMtUsd = b.PricePerMt,
            totalCostUsd = b.TotalCost,
            suppliedQtyMt = b.SuppliedQty,
            invoiceNo = b.InvoiceNo,
            invoiceAmount = b.InvoiceAmount,
            dueDate = b.DueDate,
            approvalStatus = b.ApprovalStatus,
            paymentStatus = b.PaymentStatus
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, bunkerRequirements = rows };
    }
}

internal sealed class UpdateBunkerRequirementTool(IApplicationDbContext db) : IAssistantWriteTool
{
    private static readonly string[] Statuses = ["Pending RFQ", "RFQ Sent", "Quotes Received", "Supplier Selected", "Booked"];
    private static readonly string[] Priorities = ["High", "Medium", "Low"];

    public string Name => "propose_bunker_update";
    public string Description => "Propose changing the priority, or moving the status through the early bunker steps (Pending RFQ to Booked), of a bunker requirement. Invoice, approval and payment states cannot be changed. The user must confirm before anything is saved.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("requirementNo", "string", "Requirement number, e.g. BR-2606-024.", true),
        new("field", "string", "Which field to change.", true, ["status", "priority"]),
        new("value", "string", "New status (Pending RFQ, RFQ Sent, Quotes Received, Supplier Selected, Booked) or priority (High, Medium, Low).", true)
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var no = ToolArgs.Text(args, "requirementNo");
        var field = ToolArgs.Text(args, "field");
        var value = ToolArgs.Text(args, "value");
        var allowed = field == "status" ? Statuses : field == "priority" ? Priorities : null;
        if (allowed is null) return new { error = "Only status and priority can be changed." };
        var match = allowed.FirstOrDefault(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase));
        if (match is null) return new { error = $"{field} must be one of: {string.Join(", ", allowed)}." };
        var requirement = await db.BunkerRequirements.AsNoTracking().FirstOrDefaultAsync(b => b.RequirementNo == no, ct);
        if (requirement is null) return new { error = $"No bunker requirement '{no}' was found." };

        var label = $"Set {field} of {no} ({requirement.VesselName}, {requirement.BunkerPort}) from \"{(field == "status" ? requirement.Status : requirement.Priority)}\" to \"{match}\"";
        return new AssistantProposal(label, label, JsonSerializer.Serialize(new { requirementNo = no, field, value = match }));
    }

    public async Task<string> ApplyAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var no = ToolArgs.Text(args, "requirementNo");
        var requirement = await db.BunkerRequirements.FirstOrDefaultAsync(b => b.RequirementNo == no, ct)
            ?? throw new AssistantToolException("The bunker requirement no longer exists.");
        var value = ToolArgs.Text(args, "value");
        switch (ToolArgs.Text(args, "field"))
        {
            case "status" when Statuses.Contains(value): requirement.Status = value; break;
            case "priority" when Priorities.Contains(value): requirement.Priority = value; break;
            default: throw new AssistantToolException("That change is not allowed.");
        }
        await db.SaveChangesAsync(ct);
        return $"Updated {no}.";
    }
}

internal sealed class ListEstimatesTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_estimates";
    public string Description => "List chartering voyage estimates with status, fix type, vessel, ports, cargo, freight rate, profit and TCE.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("vessel", "string", "Part of the vessel name."),
        new("status", "string", "Draft, Fixed, Reviewing or Cancelled."),
        new("port", "string", "Part of a load or discharge port name."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.VoyageEstimates.AsNoTracking().AsQueryable();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        var port = ToolArgs.Text(args, "port").ToLowerInvariant();
        if (vessel.Length > 0) query = query.Where(e => e.VesselName.ToLower().Contains(vessel));
        if (status.Length > 0) query = query.Where(e => e.Status.ToLower().Contains(status));
        if (port.Length > 0) query = query.Where(e => (e.LoadPort != null && e.LoadPort.ToLower().Contains(port)) || (e.DischargePort != null && e.DischargePort.ToLower().Contains(port)));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(e => e.CreatedAt).Take(ToolArgs.Limit(args)).Select(e => new
        {
            estimateNo = e.EstimateNo,
            vessel = e.VesselName,
            fixType = e.FixType,
            status = e.Status,
            commodity = e.Commodity,
            loadPort = e.LoadPort,
            dischargePort = e.DischargePort,
            quantityMt = e.Quantity,
            freightRateUsdPerMt = e.FreightRate,
            profitUsd = e.Profit,
            tceUsdPerDay = e.Tce
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, estimates = rows };
    }
}

internal sealed class ListCargoBookTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_cargo_book";
    public string Description => "List open cargo enquiries from the chartering cargo book: commodity, quantity, ports, laycan, terms and status.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("commodity", "string", "Part of the commodity name."),
        new("port", "string", "Part of a load or discharge port name."),
        new("status", "string", "Part of the cargo status, e.g. 'Open'."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.CargoBookEntries.AsNoTracking().AsQueryable();
        var commodity = ToolArgs.Text(args, "commodity").ToLowerInvariant();
        var port = ToolArgs.Text(args, "port").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        if (commodity.Length > 0) query = query.Where(c => c.Commodity.ToLower().Contains(commodity));
        if (port.Length > 0) query = query.Where(c => c.LoadPort.ToLower().Contains(port) || c.DischargePort.ToLower().Contains(port));
        if (status.Length > 0) query = query.Where(c => c.CargoStatus.ToLower().Contains(status));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(c => c.CreatedAt).Take(ToolArgs.Limit(args)).Select(c => new
        {
            cargoCode = c.CargoCode,
            commodity = c.Commodity,
            cargoType = c.CargoType,
            quantity = c.Quantity,
            tolerance = c.Tolerance,
            loadPort = c.LoadPort,
            dischargePort = c.DischargePort,
            laycanStart = c.LaycanStart,
            laycanEnd = c.LaycanEnd,
            terms = c.Terms,
            voyageType = c.VoyageType,
            cargoStatus = c.CargoStatus,
            commercialStatus = c.CommercialStatus,
            estimationStatus = c.EstimationStatus
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, cargoes = rows };
    }
}

internal sealed class ListTonnageBookTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_tonnage_book";
    public string Description => "List open vessels from the chartering tonnage book: vessel, type, DWT, open port and dates, employment type and status.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("vessel", "string", "Part of the vessel name."),
        new("openPort", "string", "Part of the open port or area."),
        new("status", "string", "Part of the commercial status, e.g. 'Open'."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.TonnageBookEntries.AsNoTracking().AsQueryable();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var openPort = ToolArgs.Text(args, "openPort").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        if (vessel.Length > 0) query = query.Where(t => t.VesselName.ToLower().Contains(vessel));
        if (openPort.Length > 0) query = query.Where(t => (t.OpenPort != null && t.OpenPort.ToLower().Contains(openPort)) || (t.OpenArea != null && t.OpenArea.ToLower().Contains(openPort)));
        if (status.Length > 0) query = query.Where(t => t.CommercialStatus.ToLower().Contains(status));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(t => t.CreatedAt).Take(ToolArgs.Limit(args)).Select(t => new
        {
            tonnageCode = t.TonnageCode,
            vessel = t.VesselName,
            imo = t.Imo,
            vesselType = t.VesselType,
            dwt = t.Dwt,
            flag = t.Flag,
            openArea = t.OpenArea,
            openPort = t.OpenPort,
            openDate = t.OpenDate,
            voyageType = t.VoyageType,
            source = t.Source,
            commercialStatus = t.CommercialStatus,
            owner = t.Owner
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, tonnage = rows };
    }
}
