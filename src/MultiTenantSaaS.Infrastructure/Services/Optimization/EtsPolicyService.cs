using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>EU and EEA countries whose ports bring a voyage into the EU ETS (name or ISO code, case-insensitive).</summary>
internal static class EtsCountries
{
    private static readonly HashSet<string> Members = new(StringComparer.OrdinalIgnoreCase)
    {
        "Austria", "AT", "Belgium", "BE", "Bulgaria", "BG", "Croatia", "HR", "Cyprus", "CY", "Czechia", "Czech Republic", "CZ", "Denmark", "DK",
        "Estonia", "EE", "Finland", "FI", "France", "FR", "Germany", "DE", "Greece", "GR", "Hungary", "HU", "Ireland", "IE", "Italy", "IT",
        "Latvia", "LV", "Lithuania", "LT", "Luxembourg", "LU", "Malta", "MT", "Netherlands", "The Netherlands", "NL", "Poland", "PL",
        "Portugal", "PT", "Romania", "RO", "Slovakia", "SK", "Slovenia", "SI", "Spain", "ES", "Sweden", "SE",
        "Norway", "NO", "Iceland", "IS", "Liechtenstein", "LI",
    };

    public static bool IsEuEea(string? country) => !string.IsNullOrWhiteSpace(country) && Members.Contains(country.Trim());
}

internal static class EtsPolicyMapper
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static List<EtsPhaseInDto> PhaseIn(string json)
    {
        try { return JsonSerializer.Deserialize<List<EtsPhaseInDto>>(json, Json) ?? new(); }
        catch (JsonException) { return new(); }
    }

    public static EtsPolicyDto ToDto(EtsPolicy policy) => new()
    {
        IntraEuCoveragePct = policy.IntraEuCoveragePct,
        ExtraEuCoveragePct = policy.ExtraEuCoveragePct,
        UnknownCoveragePct = policy.UnknownCoveragePct,
        PhaseIn = PhaseIn(policy.PhaseInJson),
        IsConfigured = true,
    };

    public static string Serialize(IEnumerable<EtsPhaseInDto> phaseIn) => JsonSerializer.Serialize(phaseIn.OrderBy(item => item.Year), Json);
}

public sealed class EtsPolicyService : IEtsPolicyService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public EtsPolicyService(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    public async Task<EtsPolicyDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var policy = await _db.EtsPolicies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return policy == null ? new EtsPolicyDto() : EtsPolicyMapper.ToDto(policy);
    }

    public async Task<(EtsPolicyDto? Policy, string? Problem)> SaveAsync(EtsPolicyDto dto, CancellationToken cancellationToken = default)
    {
        bool Valid(double value) => double.IsFinite(value) && value is >= 0 and <= 100;
        if (!Valid(dto.IntraEuCoveragePct) || !Valid(dto.ExtraEuCoveragePct) || !Valid(dto.UnknownCoveragePct))
            return (null, "Coverage percentages must be between 0 and 100.");
        if (dto.PhaseIn.Count is 0 or > 30) return (null, "Enter between 1 and 30 phase-in years.");
        if (dto.PhaseIn.Any(item => item.Year is < 2024 or > 2100 || !Valid(item.Pct))) return (null, "Phase-in years must be 2024-2100 with a percentage between 0 and 100.");
        if (dto.PhaseIn.Select(item => item.Year).Distinct().Count() != dto.PhaseIn.Count) return (null, "Each phase-in year may appear only once.");

        var policy = await _db.EtsPolicies.FirstOrDefaultAsync(cancellationToken);
        if (policy == null)
        {
            policy = new EtsPolicy { TenantId = _user.TenantId };
            _db.EtsPolicies.Add(policy);
        }
        policy.IntraEuCoveragePct = dto.IntraEuCoveragePct;
        policy.ExtraEuCoveragePct = dto.ExtraEuCoveragePct;
        policy.UnknownCoveragePct = dto.UnknownCoveragePct;
        policy.PhaseInJson = EtsPolicyMapper.Serialize(dto.PhaseIn);
        policy.UpdatedAtUtc = DateTime.UtcNow;
        policy.UpdatedByUserId = _user.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return (EtsPolicyMapper.ToDto(policy), null);
    }
}
