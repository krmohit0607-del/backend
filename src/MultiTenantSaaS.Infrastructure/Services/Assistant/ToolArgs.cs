using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

/// <summary>Argument parsing and shared lookups for assistant tools.</summary>
internal static class ToolArgs
{
    public static string Text(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    public static int Limit(JsonElement args, int fallback = 20, int max = 50) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty("limit", out var value) && value.TryGetInt32(out var n)
            ? Math.Clamp(n, 1, max)
            : fallback;

    public static bool Bool(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    public static AssistantParameter Limit() => new("limit", "integer", "Maximum rows to return (1-50, default 20).");

    public static double? R(double? value, int digits = 2) => value is null ? null : Math.Round(value.Value, digits);

    public static string? Day(DateTime? value) => value?.ToString("yyyy-MM-dd");

    /// <summary>The voyage the user means: the named one, else the one open on screen.</summary>
    public static async Task<VoyageRef?> ResolveVoyageAsync(IApplicationDbContext db, AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var raw = Text(args, "voyage");
        if (raw.Length == 0 && context.UiContext.ValueKind == JsonValueKind.Object
            && context.UiContext.TryGetProperty("activeVoyage", out var active) && active.ValueKind == JsonValueKind.Object)
            raw = Text(active, "id");
        if (raw.Length == 0) return null;

        var query = db.Voyages.AsNoTracking();
        var entity = Guid.TryParse(raw, out var id)
            ? await query.FirstOrDefaultAsync(v => v.Id == id || v.VoyageCode == raw, ct)
            : await query.FirstOrDefaultAsync(v => v.VoyageCode == raw, ct);
        return entity is null
            ? new VoyageRef(null, new[] { raw })
            : new VoyageRef(entity, new[] { entity.Id.ToString(), entity.VoyageCode, raw }.Distinct().ToArray());
    }

    public static readonly AssistantParameter VoyageParameter =
        new("voyage", "string", "Voyage id or voyage code. Omit to use the voyage open on screen.");

    public static object NoVoyage() => new { error = "No voyage was given and none is open on screen. Ask the user which voyage they mean (use list_voyages to find it)." };
}

/// <summary>A voyage as the assistant sees it: the stored row if there is one, plus every key other tables may use for it.</summary>
internal sealed record VoyageRef(Voyage? Entity, string[] Keys)
{
    public string Display => Entity is null ? Keys[0] : $"{Entity.VoyageCode} ({Entity.VesselName})";
}
