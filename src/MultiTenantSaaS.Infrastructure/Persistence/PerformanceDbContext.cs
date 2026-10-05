using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.Infrastructure.Persistence;

/// <summary>
/// Dedicated database context for the Performance module (tracksheets +
/// voyage performance reports) — a physically separate database from
/// <see cref="ApplicationDbContext"/> (see "PerformanceConnection" in
/// appsettings), so the module can run as its own service/database for
/// clients subscribing to it, while still tagging every row with the
/// tenant id from the shared auth token for isolation.
/// </summary>
public class PerformanceDbContext : DbContext, IPerformanceDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public PerformanceDbContext(
        DbContextOptions<PerformanceDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<TracksheetRow> TracksheetRows => Set<TracksheetRow>();
    public DbSet<VoyagePerformanceReport> PerformanceReports => Set<VoyagePerformanceReport>();

    private Guid? CurrentTenantId => _currentUserService?.TenantId;
    private string? CurrentUserRole => _currentUserService?.Role;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TracksheetRow>(entity =>
        {
            entity.ToTable("TracksheetRows");
            entity.HasIndex(r => new { r.TenantId, r.VoyageId, r.SortOrder });
            entity.Property(r => r.VoyageId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.VesselImo).HasMaxLength(20);
            entity.HasQueryFilter(r =>
                CurrentTenantId == null ||
                CurrentUserRole == Domain.Enums.UserRoles.SuperAdmin ||
                r.TenantId == CurrentTenantId);
        });

        builder.Entity<VoyagePerformanceReport>(entity =>
        {
            entity.ToTable("VoyagePerformanceReports");
            entity.HasIndex(r => new { r.TenantId, r.VoyageId }).IsUnique();
            entity.Property(r => r.VoyageId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.VesselImo).HasMaxLength(20);
            entity.Property(r => r.VesselName).HasMaxLength(200);
            entity.Property(r => r.ReportJson).HasColumnType("nvarchar(max)");
            entity.HasQueryFilter(r =>
                CurrentTenantId == null ||
                CurrentUserRole == Domain.Enums.UserRoles.SuperAdmin ||
                r.TenantId == CurrentTenantId);
        });
    }
}
