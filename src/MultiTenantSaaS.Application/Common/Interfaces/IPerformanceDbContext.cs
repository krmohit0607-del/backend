using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>
/// The Performance module's own database context — physically separate from
/// <see cref="IApplicationDbContext"/> (own connection string/server), so the
/// Performance service can be deployed, scaled and backed up independently
/// while still correlating rows back to a tenant/voyage via plain ids.
/// </summary>
public interface IPerformanceDbContext
{
    DbSet<TracksheetRow> TracksheetRows { get; }
    DbSet<VoyagePerformanceReport> PerformanceReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
