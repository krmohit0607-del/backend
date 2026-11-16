using System.Collections.Concurrent;
using System.Text.Json;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal static class LandMask
{
    private readonly record struct Point(double Lon, double Lat);
    private sealed record Ring(Point[] Points, double MinLat, double MaxLat, double MinLon, double MaxLon);
    private sealed record Polygon(Ring Outer, Ring[] Holes);

    private static readonly Lazy<Polygon[]> Polygons = new(LoadPolygons, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly ConcurrentDictionary<(double, double, double, double), bool> ArcCache = new();

    public static bool SegmentCrossesLand(double fromLat, double fromLng, double toLat, double toLng)
    {
        if (ArcCache.Count > 200_000) ArcCache.Clear();
        return ArcCache.GetOrAdd((fromLat, fromLng, toLat, toLng), key => ComputeCrossesLand(key.Item1, key.Item2, key.Item3, key.Item4));
    }

    private static bool ComputeCrossesLand(double fromLat, double fromLng, double toLat, double toLng)
    {
        var distanceNm = OptimizationGeoMath.DistanceNm(fromLat, fromLng, toLat, toLng);
        var bearing = OptimizationGeoMath.InitialBearingDeg(fromLat, fromLng, toLat, toLng);
        var samples = Math.Max(1, (int)Math.Ceiling(distanceNm / 10));
        for (var index = 0; index <= samples; index++)
        {
            var sampleDistance = distanceNm * index / samples;
            if (sampleDistance < 5 || distanceNm - sampleDistance < 5) continue;
            var point = OptimizationGeoMath.Destination(fromLat, fromLng, bearing, sampleDistance);
            if (IsLand(point.Lat, point.Lng)) return true;
        }
        return false;
    }

    private static bool IsLand(double lat, double lon)
    {
        foreach (var polygon in Polygons.Value)
        {
            if (lat < polygon.Outer.MinLat || lat > polygon.Outer.MaxLat) continue;
            if (!Contains(polygon.Outer, lat, lon)) continue;
            if (polygon.Holes.Any(hole => Contains(hole, lat, lon))) continue;
            return true;
        }
        return false;
    }

    private static bool Contains(Ring ring, double lat, double lon)
    {
        var crossesDateLine = ring.MaxLon - ring.MinLon > 180;
        if (!crossesDateLine && (lon < ring.MinLon || lon > ring.MaxLon)) return false;
        var inside = false;
        var previous = ring.Points[^1];
        foreach (var current in ring.Points)
        {
            var x1 = UnwrapLongitude(previous.Lon, lon);
            var x2 = UnwrapLongitude(current.Lon, lon);
            if ((previous.Lat > lat) != (current.Lat > lat) &&
                lon < (x2 - x1) * (lat - previous.Lat) / (current.Lat - previous.Lat) + x1)
            {
                inside = !inside;
            }
            previous = current;
        }
        return inside;
    }

    private static double UnwrapLongitude(double longitude, double reference)
    {
        while (longitude - reference > 180) longitude -= 360;
        while (longitude - reference < -180) longitude += 360;
        return longitude;
    }

    private static Polygon[] LoadPolygons()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "landPolygons.json");
        if (!File.Exists(path)) throw new InvalidOperationException("The Natural Earth land-mask data is missing from the backend deployment.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var polygons = new List<Polygon>();
        foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            var geometry = feature.GetProperty("geometry");
            var type = geometry.GetProperty("type").GetString();
            var coordinates = geometry.GetProperty("coordinates");
            if (type == "Polygon")
            {
                AddPolygon(coordinates, polygons);
            }
            else if (type == "MultiPolygon")
            {
                foreach (var polygon in coordinates.EnumerateArray()) AddPolygon(polygon, polygons);
            }
        }
        return polygons.ToArray();
    }

    private static void AddPolygon(JsonElement rings, List<Polygon> polygons)
    {
        var parsed = rings.EnumerateArray().Select(ParseRing).Where(ring => ring.Points.Length >= 3).ToArray();
        if (parsed.Length > 0) polygons.Add(new Polygon(parsed[0], parsed.Skip(1).ToArray()));
    }

    private static Ring ParseRing(JsonElement points)
    {
        var parsed = points.EnumerateArray()
            .Select(point => new Point(point[0].GetDouble(), point[1].GetDouble()))
            .ToArray();
        return new Ring(
            parsed,
            parsed.Min(point => point.Lat),
            parsed.Max(point => point.Lat),
            parsed.Min(point => point.Lon),
            parsed.Max(point => point.Lon));
    }
}
