using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

internal sealed class ListVoyagesTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_voyages";
    public string Description => "List voyages in the fleet with status, ports, ETD/ETA, client, charter-party speed and consumption. Filter by vessel, status, port or client. Use this to find a voyage id or answer fleet-level questions.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("vessel", "string", "Part of the vessel name."),
        new("status", "string", "Part of the status text, e.g. 'At Sea'."),
        new("port", "string", "Part of a load or discharge port name."),
        new("client", "string", "Part of the client name."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.Voyages.AsNoTracking();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        var port = ToolArgs.Text(args, "port").ToLowerInvariant();
        var client = ToolArgs.Text(args, "client").ToLowerInvariant();
        if (vessel.Length > 0) query = query.Where(v => v.VesselName.ToLower().Contains(vessel));
        if (status.Length > 0) query = query.Where(v => v.Status.ToLower().Contains(status));
        if (port.Length > 0) query = query.Where(v => v.PortFrom.ToLower().Contains(port) || v.PortTo.ToLower().Contains(port));
        if (client.Length > 0) query = query.Where(v => v.Client != null && v.Client.ToLower().Contains(client));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(v => v.Eta).Take(ToolArgs.Limit(args)).Select(v => new
        {
            id = v.Id,
            code = v.VoyageCode,
            vessel = v.VesselName,
            imo = v.Imo,
            status = v.Status,
            openStatus = v.OpenStatus,
            portFrom = v.PortFrom,
            portTo = v.PortTo,
            etd = v.Etd,
            eta = v.Eta,
            client = v.Client,
            pic = v.Pic,
            cpSpeedKn = v.CpSpeed,
            cpConsMtPerDay = v.CpCons,
            aiAlert = v.AiAlert
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, voyages = rows };
    }
}

internal sealed class GetVoyageTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "get_voyage";
    public string Description => "Get the stored details of one voyage: vessel, ports, schedule, client, charter-party and instructed speed/consumption, pricing and costs, notes.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [ToolArgs.VoyageParameter];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();
        var v = voyage.Entity;
        if (v is null) return new { error = $"No stored voyage matches '{voyage.Keys[0]}'." };
        return new
        {
            id = v.Id,
            code = v.VoyageCode,
            vessel = v.VesselName,
            imo = v.Imo,
            vesselType = v.VesselType,
            flag = v.Flag,
            dwt = v.Dwt,
            status = v.Status,
            openStatus = v.OpenStatus,
            priority = v.Priority,
            portFrom = v.PortFrom,
            portTo = v.PortTo,
            interimPort = v.InterimPort,
            etd = v.Etd,
            eta = v.Eta,
            lastNoon = v.LastNoon,
            routeRef = v.RouteRef,
            client = v.Client,
            pic = v.Pic,
            service = v.Service,
            cpSpeedKn = v.CpSpeed,
            cpConsMtPerDay = v.CpCons,
            instructedSpeedKn = v.InstSpeed,
            instructedConsMtPerDay = v.InstCons,
            price = v.Price,
            pricingBasis = v.PricingBasis,
            costPerDayUsd = v.CostPerDay,
            foCostUsd = v.FoCost,
            goCostUsd = v.GoCost,
            euaCostUsd = v.EuaCost,
            openTasks = v.OpenTasks,
            aiAlert = v.AiAlert,
            handoverNote = v.HandoverNote
        };
    }
}

internal sealed class UpdateVoyageTool(IApplicationDbContext db) : IAssistantWriteTool
{
    private static readonly string[] Fields = ["status", "priority", "handoverNote", "pic"];
    private static readonly string[] Priorities = ["LOW", "MEDIUM", "HIGH"];

    public string Name => "propose_voyage_update";
    public string Description => "Propose changing the status, priority, handover note or person in charge of a stored voyage. The user must confirm before anything is saved.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        ToolArgs.VoyageParameter,
        new("field", "string", "Which field to change.", true, Fields),
        new("value", "string", "The new value. Priority must be LOW, MEDIUM or HIGH.", true)
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage?.Entity is null) return new { error = "That voyage is not stored in the database, so it cannot be updated here." };
        var field = ToolArgs.Text(args, "field");
        var value = ToolArgs.Text(args, "value");
        if (!Fields.Contains(field)) return new { error = "That field cannot be changed by the agent." };
        if (value.Length == 0 || value.Length > 500) return new { error = "The value must contain 1 to 500 characters." };
        if (field == "priority")
        {
            value = value.ToUpperInvariant();
            if (!Priorities.Contains(value)) return new { error = "Priority must be LOW, MEDIUM or HIGH." };
        }
        if (field is "status" or "pic" && value.Length > 100) return new { error = "That value is too long." };

        var label = $"Set {field} of {voyage.Entity.VoyageCode} to \"{value}\"";
        var normalized = JsonSerializer.Serialize(new { voyage = voyage.Entity.Id, field, value });
        return new AssistantProposal(label, label, normalized);
    }

    public async Task<string> ApplyAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        if (!Guid.TryParse(ToolArgs.Text(args, "voyage"), out var id)) throw new AssistantToolException("The voyage could not be identified.");
        var voyage = await db.Voyages.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AssistantToolException("The voyage no longer exists.");
        var value = ToolArgs.Text(args, "value");
        switch (ToolArgs.Text(args, "field"))
        {
            case "status": voyage.Status = value; break;
            case "priority": voyage.Priority = value; break;
            case "handoverNote": voyage.HandoverNote = value; break;
            case "pic": voyage.Pic = value; break;
            default: throw new AssistantToolException("That field cannot be changed by the agent.");
        }
        await db.SaveChangesAsync(ct);
        return $"Updated {voyage.VoyageCode}.";
    }
}

internal sealed class ListVesselsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_vessels";
    public string Description => "Search the vessel master by name, IMO, type or flag and list key particulars (type, flag, built, DWT, owner, operator, service speed).";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("query", "string", "Part of the vessel name or IMO number."),
        new("type", "string", "Part of the vessel type, e.g. 'bulk'."),
        new("flag", "string", "Part of the flag state."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.Vessels.AsNoTracking().Where(v => v.IsActive);
        var q = ToolArgs.Text(args, "query").ToLowerInvariant();
        var type = ToolArgs.Text(args, "type").ToLowerInvariant();
        var flag = ToolArgs.Text(args, "flag").ToLowerInvariant();
        if (q.Length > 0) query = query.Where(v => v.Name.ToLower().Contains(q) || v.Imo.Contains(q));
        if (type.Length > 0) query = query.Where(v => v.VesselType != null && v.VesselType.ToLower().Contains(type));
        if (flag.Length > 0) query = query.Where(v => v.Flag != null && v.Flag.ToLower().Contains(flag));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(v => v.Name).Take(ToolArgs.Limit(args)).Select(v => new
        {
            name = v.Name,
            imo = v.Imo,
            type = v.VesselType,
            flag = v.Flag,
            built = v.BuiltYear,
            deadweightMt = v.Deadweight,
            owner = v.Owner,
            @operator = v.Operator,
            serviceSpeedKn = v.ServiceSpeed
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, vessels = rows };
    }
}

internal sealed class GetVesselTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "get_vessel";
    public string Description => "Get one vessel's full particulars: dimensions, engine, speed and RPM limits, telegraph speeds, weather safety limits and performance profile.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [new("vessel", "string", "Vessel name or IMO number.", true)];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var q = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        if (q.Length == 0) return new { error = "Give a vessel name or IMO number." };
        var v = await db.Vessels.AsNoTracking().Where(x => x.Imo == q || x.Name.ToLower() == q).FirstOrDefaultAsync(ct)
            ?? await db.Vessels.AsNoTracking().Where(x => x.Name.ToLower().Contains(q)).OrderBy(x => x.Name).FirstOrDefaultAsync(ct);
        if (v is null) return new { error = $"No vessel matches '{q}'." };
        return new
        {
            name = v.Name, imo = v.Imo, type = v.VesselType, flag = v.Flag, owner = v.Owner, @operator = v.Operator, classSociety = v.ClassSociety,
            built = v.BuiltYear, iceClass = v.IceClass,
            dimensions = new { gt = v.Gt, loaM = v.LengthOverall, lbpM = v.LengthBp, beamM = v.BreadthMoulded, depthM = v.Depth, draughtM = v.Draught, deadweightMt = v.Deadweight, displacementMt = v.Displacement, holds = v.Holds, teu = v.Teu },
            engine = new { model = v.EngineModel, meType = v.MeType, mainEngineKw = v.TotalKwMainEng, fuelConsMainEng = v.FuelConsMainEng, serviceSpeedKn = v.ServiceSpeed, auxKw = v.AuxEngineTotalKw },
            limits = new { minRpm = v.MinRpm, maxRpm = v.MaxRpm, minMcr = v.MinMcr, maxMcr = v.MaxMcr, minSpeedKn = v.MinSpeed, maxSpeedKn = v.MaxSpeed, criticalRpmMin = v.CriticalRpmMin, criticalRpmMax = v.CriticalRpmMax },
            drafts = new { ballastM = v.DefaultBallastDraft, ladenM = v.DefaultLadenDraft, summerM = v.SummerDraft },
            telegraph = new { deadSlowRpm = v.DeadSlowRpm, slowAheadRpm = v.SlowAheadRpm, halfAheadRpm = v.HalfAheadRpm, fullAheadRpm = v.FullAheadRpm, fullAheadSpeedBallastKn = v.FullAheadSpeedBallast, fullAheadSpeedLadenKn = v.FullAheadSpeedLaden },
            weatherSafetyLimits = new { maxSwhBallastM = v.WslMaxSwhBallast, maxSwhLadenM = v.WslMaxSwhLaden, maxWindBallast = v.WslMaxWindsBallast, maxWindLaden = v.WslMaxWindsLaden },
            scrubber = v.Scrubber,
            hasLoadDiagram = !string.IsNullOrWhiteSpace(v.LoadDiagramJson)
        };
    }
}

internal sealed class ListClientsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_clients";
    public string Description => "List accounts (owners, charterers, brokers) and service providers (agents, suppliers) with category, location and compliance status. Contact and bank details are not available through this tool.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("query", "string", "Part of the name."),
        new("kind", "string", "Account or ServiceProvider.", false, ["Account", "ServiceProvider"]),
        new("category", "string", "Part of the category, e.g. 'Charterer'."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.Clients.AsNoTracking().AsQueryable();
        var q = ToolArgs.Text(args, "query").ToLowerInvariant();
        var kind = ToolArgs.Text(args, "kind");
        var category = ToolArgs.Text(args, "category").ToLowerInvariant();
        if (q.Length > 0) query = query.Where(c => c.Name.ToLower().Contains(q));
        if (kind.Length > 0) query = query.Where(c => c.Kind == kind);
        if (category.Length > 0) query = query.Where(c => c.Category.ToLower().Contains(category));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(c => c.Name).Take(ToolArgs.Limit(args)).Select(c => new
        {
            name = c.Name,
            kind = c.Kind,
            category = c.Category,
            location = c.Location,
            complianceStatus = c.ComplianceStatus,
            active = c.IsActive,
            pic = c.PicAssignment
        }).ToListAsync(ct);
        return new { total, returned = rows.Count, clients = rows };
    }
}
