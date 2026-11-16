using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

internal sealed class GetTracksheetSummaryTool(IApplicationDbContext db, IPerformanceDbContext perf) : IAssistantTool
{
    public string Name => "get_tracksheet_summary";
    public string Description => "Summarize a voyage's tracksheet (noon-report grid in the Performance module): number of rows, first and last position/time, distance, steaming hours, average speed, VLSFO/LSMGO ROB and consumption, and the latest rows.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [ToolArgs.VoyageParameter];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();
        var keys = voyage.Keys;
        var rows = await perf.TracksheetRows.AsNoTracking().Where(r => keys.Contains(r.VoyageId) && !r.IsDefault).OrderBy(r => r.SortOrder).ToListAsync(ct);
        if (rows.Count == 0) return new { voyage = voyage.Display, error = "No tracksheet rows are saved for this voyage." };

        var first = rows[0];
        var last = rows[^1];
        double? Consumed(Func<Domain.Entities.Performance.TracksheetRow, double?> rob, Func<Domain.Entities.Performance.TracksheetRow, double?> bunkered) =>
            rob(first) is null || rob(last) is null ? null : rob(first) + rows.Skip(1).Sum(r => bunkered(r) ?? 0) - rob(last);
        var speeds = rows.Where(r => r.AvgSpeedO is > 0).Select(r => r.AvgSpeedO!.Value).ToList();

        return new
        {
            voyage = voyage.Display,
            note = "Values are as entered in the tracksheet. Consumed fuel = first ROB + bunkers taken after the first row - last ROB.",
            rowCount = rows.Count,
            first = new { first.Date, first.Time, first.Lat, first.Lng },
            last = new { last.Date, last.Time, last.Lat, last.Lng, last.NextPort },
            steamingHours = ToolArgs.R(rows.Sum(r => r.Hrs ?? 0)),
            reportedDistanceNm = ToolArgs.R(rows.Sum(r => r.DistR ?? 0)),
            observedDistanceNm = ToolArgs.R(rows.Sum(r => r.DistO ?? 0)),
            averageSpeedKn = speeds.Count == 0 ? null : ToolArgs.R(speeds.Average()),
            vlsfo = new { firstRobMt = first.VlsfoRob, lastRobMt = last.VlsfoRob, consumedMt = ToolArgs.R(Consumed(r => r.VlsfoRob, r => r.VlsfoBunkered), 3) },
            lsmgo = new { firstRobMt = first.LsmgoRob, lastRobMt = last.LsmgoRob, consumedMt = ToolArgs.R(Consumed(r => r.LsmgoRob, r => r.LsmgoBunkered), 3) },
            latestRows = rows.TakeLast(5).Select(r => new
            {
                r.Date, r.Time, r.NextPort, r.Lat, r.Lng, hours = r.Hrs, distanceNm = r.DistR, speedKn = r.AvgSpeedO,
                vlsfoRobMt = r.VlsfoRob, lsmgoRobMt = r.LsmgoRob, r.Rpm, wind = r.WindO, waves = r.WavesO
            }).ToList()
        };
    }
}

internal sealed class GetPerformanceReportTool(IApplicationDbContext db, IPerformanceDbContext perf) : IAssistantTool
{
    public string Name => "get_performance_report";
    public string Description => "Read the saved end-of-voyage Performance Report for a voyage: cover details, totals, good-weather analysis, speed summary and VLSFO/LSMGO analysis.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [ToolArgs.VoyageParameter];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();
        var keys = voyage.Keys;
        var report = await perf.PerformanceReports.AsNoTracking().Where(r => keys.Contains(r.VoyageId)).OrderByDescending(r => r.UpdatedAt).FirstOrDefaultAsync(ct);
        if (report is null) return new { voyage = voyage.Display, error = "No Performance Report has been saved for this voyage. The Performance module can generate one from the tracksheet." };

        JsonNode? Part(JsonNode? root, string name) => (root as JsonObject)?[name]?.DeepClone();
        JsonNode? parsed;
        try { parsed = JsonNode.Parse(report.ReportJson); }
        catch (JsonException) { return new { error = "The saved report could not be read." }; }
        return new
        {
            voyage = voyage.Display,
            savedAtUtc = report.UpdatedAt ?? report.CreatedAt,
            meta = Part(parsed, "meta"),
            totals = Part(parsed, "totals"),
            goodWeather = Part(parsed, "goodWeather"),
            speed = Part(parsed, "speed"),
            vlsfo = Part(parsed, "vlsfo"),
            lsmgo = Part(parsed, "lsmgo")
        };
    }
}

internal sealed class ListVesselReportsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_vessel_reports";
    public string Description => "List the latest vessel reports from the ship (noon, departure, arrival, BOSP, EOSP, bunkering): position, ports, speed, consumption, ROB and remarks.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("vessel", "string", "Part of the vessel name."),
        new("voyageCode", "string", "Exact voyage code."),
        new("reportType", "string", "Noon, Departure, Arrival, BOSP, EOSP, Shifting or Bunkering."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.VesselReports.AsNoTracking().AsQueryable();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var code = ToolArgs.Text(args, "voyageCode");
        var type = ToolArgs.Text(args, "reportType");
        if (vessel.Length > 0) query = query.Where(r => r.VesselName.ToLower().Contains(vessel));
        if (code.Length > 0) query = query.Where(r => r.VoyageCode == code);
        if (type.Length > 0) query = query.Where(r => r.ReportType == type);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(r => r.ReportDateTime).Take(ToolArgs.Limit(args, 10)).Select(r => new
        {
            reportNo = r.ReportNo,
            type = r.ReportType,
            vessel = r.VesselName,
            voyageCode = r.VoyageCode,
            timeUtc = r.ReportDateTime,
            lat = r.Latitude,
            lng = r.Longitude,
            currentPort = r.CurrentPort,
            nextPort = r.NextPort,
            etaNextPort = r.EtaNextPort,
            steamingHours = r.SteamingHours,
            distanceObservedNm = r.DistanceObserved,
            speedObservedKn = r.SpeedObserved,
            rpm = r.Rpm,
            vlsfoConsMt = r.VlsfoCons,
            vlsfoRobMt = r.VlsfoRob,
            lsmgoConsMt = r.LsmgoCons,
            lsmgoRobMt = r.LsmgoRob,
            windForce = r.WindForce,
            seaState = r.SeaState,
            remarks = r.Remarks
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, reports = rows };
    }
}

internal sealed class GetEmissionsTool(IApplicationDbContext db) : IAssistantTool
{
    private const int MaxBlob = 4_000;

    public string Name => "get_emissions";
    public string Description => "Read the Emissions module records (EU ETS / CII / FuelEU data) for a voyage or vessel: compliance year, EUA price, CO2 adjustment and the stored compliance and metrics data.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        ToolArgs.VoyageParameter,
        new("vessel", "string", "Part of the vessel name, instead of a voyage."),
        new("year", "string", "Compliance year, e.g. 2026.")
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.EmissionsRecords.AsNoTracking().AsQueryable();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var year = ToolArgs.Text(args, "year");
        if (vessel.Length > 0) query = query.Where(e => e.VesselName.ToLower().Contains(vessel));
        else
        {
            var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
            if (voyage is not null)
            {
                var keys = voyage.Keys;
                query = query.Where(e => keys.Contains(e.VoyageCode));
            }
        }
        if (year.Length > 0) query = query.Where(e => e.ComplianceYear == year);

        var records = await query.OrderByDescending(e => e.CreatedAt).Take(5).ToListAsync(ct);
        if (records.Count == 0) return new { error = "No emissions records were found for that voyage or vessel." };
        return new
        {
            records = records.Select(e => new
            {
                voyageCode = e.VoyageCode,
                vessel = e.VesselName,
                complianceYear = e.ComplianceYear,
                trade = e.Trade,
                euaPriceEur = e.EuaPriceEur,
                co2AdjustmentT = e.Co2AdjustmentT,
                approvedBy = e.ApprovedBy,
                approvedDate = e.ApprovedDate,
                compliance = Blob(e.ComplianceJson),
                metrics = Blob(e.MetricsJson),
                adjustments = Blob(e.AdjustmentsJson)
            }).ToList()
        };
    }

    private static object? Blob(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return json.Length > MaxBlob ? new { truncated = true, data = json[..MaxBlob] } : JsonNode.Parse(json);
    }
}

internal sealed class ListRecommendationsTool(IApplicationDbContext db, IOptimizationMonitoringService monitoring) : IAssistantTool
{
    public string Name => "list_optimization_recommendations";
    public string Description => "List re-optimization suggestions raised for a voyage when the vessel drifted from its stored optimized route (status, reasons, time).";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        ToolArgs.VoyageParameter,
        new("status", "string", "Pending, Confirmed or Dismissed.", false, ["Pending", "Confirmed", "Dismissed"])
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();
        var status = ToolArgs.Text(args, "status");
        foreach (var key in voyage.Keys)
        {
            var rows = await monitoring.GetRecommendationsAsync(key, status.Length == 0 ? null : status, ct);
            if (rows.Count > 0) return new { voyage = voyage.Display, recommendations = rows.Take(20).ToList() };
        }
        return new { voyage = voyage.Display, recommendations = Array.Empty<object>(), note = "None found." };
    }
}

internal sealed class SearchRecordsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "search_records";
    public string Description => "Search across voyages, vessels, accounts, bunker requirements, financial transactions and estimates by free text (name, code, invoice, port, counterparty). Use this when you do not know which module holds something.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [new("query", "string", "Text to find (at least 2 characters).", true)];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var q = ToolArgs.Text(args, "query").ToLowerInvariant();
        if (q.Length < 2) return new { error = "Give at least 2 characters to search for." };

        var voyages = await db.Voyages.AsNoTracking()
            .Where(v => v.VoyageCode.ToLower().Contains(q) || v.VesselName.ToLower().Contains(q) || v.PortFrom.ToLower().Contains(q) || v.PortTo.ToLower().Contains(q) || (v.Client != null && v.Client.ToLower().Contains(q)))
            .Take(5).Select(v => new { id = v.Id, code = v.VoyageCode, vessel = v.VesselName, v.PortFrom, v.PortTo, v.Status }).ToListAsync(ct);
        var vessels = await db.Vessels.AsNoTracking()
            .Where(v => v.Name.ToLower().Contains(q) || v.Imo.Contains(q))
            .Take(5).Select(v => new { v.Name, v.Imo, type = v.VesselType }).ToListAsync(ct);
        var clients = await db.Clients.AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(q))
            .Take(5).Select(c => new { c.Name, c.Kind, c.Category }).ToListAsync(ct);
        var bunkers = await db.BunkerRequirements.AsNoTracking()
            .Where(b => b.RequirementNo.ToLower().Contains(q) || b.VesselName.ToLower().Contains(q) || b.BunkerPort.ToLower().Contains(q) || (b.InvoiceNo != null && b.InvoiceNo.ToLower().Contains(q)))
            .Take(5).Select(b => new { b.RequirementNo, vessel = b.VesselName, port = b.BunkerPort, b.Status }).ToListAsync(ct);
        var transactions = await db.FinancialTransactions.AsNoTracking()
            .Where(t => t.TransactionNo.ToLower().Contains(q) || t.InvoiceNo.ToLower().Contains(q) || t.Counterparty.ToLower().Contains(q) || t.VesselName.ToLower().Contains(q) || t.Reference.ToLower().Contains(q))
            .Take(5).Select(t => new { t.TransactionNo, t.Kind, t.Category, t.Counterparty, t.InvoiceNo, t.Currency, t.Amount, t.Status }).ToListAsync(ct);
        var estimates = await db.VoyageEstimates.AsNoTracking()
            .Where(e => e.EstimateNo.ToLower().Contains(q) || e.VesselName.ToLower().Contains(q))
            .Take(5).Select(e => new { e.EstimateNo, vessel = e.VesselName, e.Status, e.LoadPort, e.DischargePort }).ToListAsync(ct);

        return new { query = q, voyages, vessels, accounts = clients, bunkerRequirements = bunkers, financialTransactions = transactions, estimates, note = "At most 5 hits per type. Use the specific list or get tool for detail." };
    }
}
