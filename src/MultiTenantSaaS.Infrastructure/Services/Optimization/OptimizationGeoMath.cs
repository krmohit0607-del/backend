namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Small great-circle / polygon geometry helpers for the route optimization engine — plain-C# equivalents of the point-in-polygon and distance/bearing primitives the TNM OptiNav C++ reference engine gets from GDAL/OGR, scoped to exactly what the engine needs (no GIS library dependency).</summary>
internal static class OptimizationGeoMath
{
    private const double EarthRadiusNm = 3440.065;

    public static double ToRadians(double deg) => deg * Math.PI / 180.0;
    public static double ToDegrees(double rad) => rad * 180.0 / Math.PI;

    /// <summary>Great-circle distance in nautical miles (haversine).</summary>
    public static double DistanceNm(double lat1, double lng1, double lat2, double lng2)
    {
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dPhi = ToRadians(lat2 - lat1);
        var dLambda = ToRadians(lng2 - lng1);
        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusNm * c;
    }

    /// <summary>Initial bearing (degrees, 0=N clockwise) from point 1 to point 2.</summary>
    public static double InitialBearingDeg(double lat1, double lng1, double lat2, double lng2)
    {
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dLambda = ToRadians(lng2 - lng1);
        var y = Math.Sin(dLambda) * Math.Cos(phi2);
        var x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(dLambda);
        var theta = Math.Atan2(y, x);
        return (ToDegrees(theta) + 360.0) % 360.0;
    }

    /// <summary>Destination point given a start, bearing (deg) and distance (nm).</summary>
    public static (double Lat, double Lng) Destination(double lat, double lng, double bearingDeg, double distanceNm)
    {
        var delta = distanceNm / EarthRadiusNm;
        var theta = ToRadians(bearingDeg);
        var phi1 = ToRadians(lat);
        var lambda1 = ToRadians(lng);

        var phi2 = Math.Asin(Math.Sin(phi1) * Math.Cos(delta) + Math.Cos(phi1) * Math.Sin(delta) * Math.Cos(theta));
        var lambda2 = lambda1 + Math.Atan2(
            Math.Sin(theta) * Math.Sin(delta) * Math.Cos(phi1),
            Math.Cos(delta) - Math.Sin(phi1) * Math.Sin(phi2));

        return (ToDegrees(phi2), ((ToDegrees(lambda2) + 540.0) % 360.0) - 180.0);
    }

    /// <summary>Fold an absolute relative-bearing difference (deg) to 0 (head-on) .. 180 (directly astern) — matches the WeatherFactorEntry calibration table's angle convention.</summary>
    public static double FoldRelativeAngle(double vesselCourseDeg, double weatherFromDeg)
    {
        var diff = Math.Abs(((weatherFromDeg - vesselCourseDeg) % 360.0 + 360.0) % 360.0);
        return diff > 180.0 ? 360.0 - diff : diff;
    }

    /// <summary>Ray-casting point-in-polygon test. `ring` is an ordered list of [lat, lng] vertices (need not be explicitly closed).</summary>
    public static bool PointInRing(double lat, double lng, IReadOnlyList<double[]> ring)
    {
        var inside = false;
        var n = ring.Count;
        if (n < 3) return false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var yi = ring[i][0]; var xi = ring[i][1];
            var yj = ring[j][0]; var xj = ring[j][1];
            var intersects = ((yi > lat) != (yj > lat)) &&
                              (lng < (xj - xi) * (lat - yi) / ((yj - yi) == 0 ? 1e-12 : (yj - yi)) + xi);
            if (intersects) inside = !inside;
        }
        return inside;
    }

    public static bool PointInAnyRing(double lat, double lng, IEnumerable<List<double[]>> rings)
    {
        foreach (var ring in rings)
        {
            if (PointInRing(lat, lng, ring)) return true;
        }
        return false;
    }
}
