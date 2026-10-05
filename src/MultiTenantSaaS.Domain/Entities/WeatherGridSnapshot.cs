using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// Persistent cache of one weather factor's grid over a fixed-size
/// geographic tile at a specific UTC hour. Deliberately NOT tenant-scoped
/// (no <see cref="IHasTenant"/>) — weather is the same for every tenant, so
/// sharing one global cache (instead of duplicating it per tenant) is both
/// correct and keeps storage minimal. Once a (factor, hour, tile) triple is
/// fetched, every future request for any overlapping viewport — from any
/// user, any tenant — is served from this table instead of re-calling the
/// upstream weather API.
/// </summary>
public class WeatherGridSnapshot : BaseEntity
{
    public string FactorId { get; set; } = string.Empty;

    /// <summary>UTC hour this snapshot represents (minutes/seconds zeroed).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>South-west origin of the tile, in degrees.</summary>
    public double TileSouth { get; set; }
    public double TileWest { get; set; }

    /// <summary>Tile edge length in degrees (fixed per deployment, see `WeatherTileService`).</summary>
    public double TileSizeDeg { get; set; }

    /// <summary>Grid points per tile edge (e.g. 11 = an 11x11 sample grid).</summary>
    public int Resolution { get; set; }

    /// <summary>
    /// Base64 of a little-endian Int16 array (magnitude values scaled x10,
    /// so e.g. 12.3 kt is stored as 123) — row-major, south-to-north rows,
    /// west-to-east within each row. Int16 instead of double/JSON keeps each
    /// tile at a few hundred bytes instead of several KB.
    /// </summary>
    public string MagnitudeData { get; set; } = string.Empty;

    /// <summary>Same layout as <see cref="MagnitudeData"/> but whole compass
    /// degrees (0-359); null for non-directional factors.</summary>
    public string? DirectionData { get; set; }

    public DateTime FetchedAtUtc { get; set; }
}
