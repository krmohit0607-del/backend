using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal sealed record PortCandidate(string Name, string Country, double Lat, double Lng);

/// <summary>Ranks ports near the chosen route by the cost of deviating to bunker there. Every port uses the run's fuel prices, so only the detour (time, fuel, hire) separates them; availability and supplier quotes are not checked.</summary>
internal static class BunkerPortComparison
{
    public const double MaxOffRouteNm = 150;
    public const int MaxOptions = 8;

    public static List<BunkerPortOptionDto> Compare(
        List<RouteWaypointDto> waypoints, IEnumerable<PortCandidate> ports, IReadOnlyList<SupplyPoint> bookedSupplies,
        OptimizationRequestDto request, double totalHours, double mainFuelMt, double distanceNm)
    {
        if (waypoints.Count < 2 || totalHours <= 0 || distanceNm <= 0) return new();
        var sogKn = distanceNm / totalHours;
        var fuelPerHour = mainFuelMt / totalHours;

        var options = new List<BunkerPortOptionDto>();
        foreach (var port in ports)
        {
            var (along, cross, segment, fraction) = OptimizationMonitoringService.Project(waypoints, port.Lat, port.Lng);
            if (cross > MaxOffRouteNm) continue;

            var detourNm = 2 * cross;
            var extraHours = detourNm / sogKn;
            var extraFuel = extraHours * fuelPerHour;
            var a = waypoints[segment];
            var b = waypoints[segment + 1];
            options.Add(new BunkerPortOptionDto
            {
                Port = port.Name,
                Country = port.Country,
                Lat = port.Lat,
                Lng = port.Lng,
                DistanceOffRouteNm = cross,
                ExtraDistanceNm = detourNm,
                ExtraHours = extraHours,
                ExtraFuelMt = extraFuel,
                ExtraCostUsd = extraFuel * request.FoPriceUsdPerMt + request.HireRatePerDayUsd / 24 * extraHours,
                AlongRouteNm = along,
                EtaAtClosestPointUtc = a.EtaUtc + (b.EtaUtc - a.EtaUtc) * fraction,
                RobVlsfoAtPointMt = Lerp(a.RobVlsfoMt, b.RobVlsfoMt, fraction),
                RobLsmgoAtPointMt = Lerp(a.RobLsmgoMt, b.RobLsmgoMt, fraction),
                HasBookedSupply = bookedSupplies.Any(supply => Math.Abs(supply.Lat - port.Lat) < 0.05 && Math.Abs(supply.Lng - port.Lng) < 0.05),
            });
        }

        return options.OrderBy(option => option.ExtraCostUsd).Take(MaxOptions).ToList();
    }

    private static double? Lerp(double? from, double? to, double t) => from.HasValue && to.HasValue ? from.Value + (to.Value - from.Value) * t : null;
}
