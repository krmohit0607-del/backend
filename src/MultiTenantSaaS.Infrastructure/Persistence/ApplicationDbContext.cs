using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Common;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;
using Module = MultiTenantSaaS.Domain.Entities.Module;

namespace MultiTenantSaaS.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<TenantModuleAccess> TenantModuleAccesses => Set<TenantModuleAccess>();
    public DbSet<EmployeeModulePermission> EmployeeModulePermissions => Set<EmployeeModulePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<VoyageOrder> VoyageOrders => Set<VoyageOrder>();
    public DbSet<Voyage> Voyages => Set<Voyage>();
    public DbSet<Passage> Passages => Set<Passage>();
    public DbSet<PassageLeg> PassageLegs => Set<PassageLeg>();
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<VesselHistory> VesselHistories => Set<VesselHistory>();
    public DbSet<VoyageEstimate> VoyageEstimates => Set<VoyageEstimate>();
    public DbSet<CargoBookEntry> CargoBookEntries => Set<CargoBookEntry>();
    public DbSet<TonnageBookEntry> TonnageBookEntries => Set<TonnageBookEntry>();
    public DbSet<BunkerRequirement> BunkerRequirements => Set<BunkerRequirement>();
    public DbSet<EmissionsRecord> EmissionsRecords => Set<EmissionsRecord>();
    public DbSet<EmissionsScenario> EmissionsScenarios => Set<EmissionsScenario>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<VesselReport> VesselReports => Set<VesselReport>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<ImoShip> ImoShips => Set<ImoShip>();
    public DbSet<Port> Ports => Set<Port>();
    
    // --- Voyage Operations (Tier 1) ---
    public DbSet<VoyageRecap> VoyageRecaps => Set<VoyageRecap>();
    
    // --- Postfix Settlement (Tier 1) ---
    public DbSet<ProFormaDisbursement> ProFormaDisbursements => Set<ProFormaDisbursement>();
    public DbSet<FinalDisbursement> FinalDisbursements => Set<FinalDisbursement>();
    public DbSet<AgentInvoice> AgentInvoices => Set<AgentInvoice>();
    public DbSet<AdditionalService> AdditionalServices => Set<AdditionalService>();
    public DbSet<ClaimRecord> ClaimRecords => Set<ClaimRecord>();
    public DbSet<HirePayment> HirePayments => Set<HirePayment>();
    public DbSet<SettlementMilestone> SettlementMilestones => Set<SettlementMilestone>();
    
    // --- Client Management (Tier 1) ---
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();
    
    // --- Reference Data & Configuration ---
    public DbSet<LaytimeCalculation> LaytimeCalculations => Set<LaytimeCalculation>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailDistributionList> EmailDistributionLists => Set<EmailDistributionList>();
    public DbSet<EnumerationValue> EnumerationValues => Set<EnumerationValue>();
    public DbSet<CargoMaster> CargoMasters => Set<CargoMaster>();
    public DbSet<AreaConstraint> AreaConstraints => Set<AreaConstraint>();
    public DbSet<SavedPassage> SavedPassages => Set<SavedPassage>();
    public DbSet<WorkflowConfiguration> WorkflowConfigurations => Set<WorkflowConfiguration>();
    public DbSet<WeatherGridSnapshot> WeatherGridSnapshots => Set<WeatherGridSnapshot>();

    public Guid? CurrentTenantId => _currentUserService?.TenantId;
    public string? CurrentUserRole => _currentUserService?.Role;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations in this assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure Global Query Filters for Multi-Tenant Data Isolation
        builder.Entity<ApplicationUser>().HasQueryFilter(u =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            u.TenantId == CurrentTenantId);

        builder.Entity<TenantModuleAccess>().HasQueryFilter(t =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            t.TenantId == CurrentTenantId);

        builder.Entity<EmployeeModulePermission>().HasQueryFilter(p =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            p.TenantId == CurrentTenantId);

        builder.Entity<AuditLog>().HasQueryFilter(a =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            a.TenantId == CurrentTenantId);

        builder.Entity<VoyageOrder>().HasQueryFilter(o =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            o.TenantId == CurrentTenantId);

        builder.Entity<Voyage>().HasQueryFilter(v =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            v.TenantId == CurrentTenantId);

        builder.Entity<Passage>().HasQueryFilter(p =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            p.TenantId == CurrentTenantId);

        builder.Entity<Vessel>().HasQueryFilter(v =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            v.TenantId == CurrentTenantId);

        builder.Entity<VesselHistory>().HasQueryFilter(h =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            h.TenantId == CurrentTenantId);

        builder.Entity<VoyageEstimate>().HasQueryFilter(e =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            e.TenantId == CurrentTenantId);

        builder.Entity<CargoBookEntry>().HasQueryFilter(c =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            c.TenantId == CurrentTenantId);

        builder.Entity<TonnageBookEntry>().HasQueryFilter(t =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            t.TenantId == CurrentTenantId);

        builder.Entity<BunkerRequirement>().HasQueryFilter(b =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            b.TenantId == CurrentTenantId);

        builder.Entity<EmissionsRecord>().HasQueryFilter(e =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            e.TenantId == CurrentTenantId);

        builder.Entity<EmissionsScenario>().HasQueryFilter(e =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            e.TenantId == CurrentTenantId);

        builder.Entity<FinancialTransaction>().HasQueryFilter(f =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            f.TenantId == CurrentTenantId);

        builder.Entity<VesselReport>().HasQueryFilter(r =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            r.TenantId == CurrentTenantId);

        builder.Entity<UserSetting>().HasQueryFilter(s =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            s.TenantId == CurrentTenantId);

        builder.Entity<ImoShip>().HasQueryFilter(i =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            i.TenantId == CurrentTenantId);

        builder.Entity<Port>().HasQueryFilter(p =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            p.TenantId == CurrentTenantId);

        // New Tier 1 & 2 Entities - Multi-tenant Query Filters
        builder.Entity<VoyageRecap>().HasQueryFilter(vr =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            vr.TenantId == CurrentTenantId);

        builder.Entity<ProFormaDisbursement>().HasQueryFilter(pda =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            pda.TenantId == CurrentTenantId);

        builder.Entity<FinalDisbursement>().HasQueryFilter(fda =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            fda.TenantId == CurrentTenantId);

        builder.Entity<AgentInvoice>().HasQueryFilter(ai =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            ai.TenantId == CurrentTenantId);

        builder.Entity<AdditionalService>().HasQueryFilter(svc =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            svc.TenantId == CurrentTenantId);

        builder.Entity<ClaimRecord>().HasQueryFilter(c =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            c.TenantId == CurrentTenantId);

        builder.Entity<HirePayment>().HasQueryFilter(h =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            h.TenantId == CurrentTenantId);

        builder.Entity<SettlementMilestone>().HasQueryFilter(sm =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            sm.TenantId == CurrentTenantId);

        builder.Entity<Client>().HasQueryFilter(cl =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            cl.TenantId == CurrentTenantId);

        builder.Entity<ClientContact>().HasQueryFilter(cc =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            cc.TenantId == CurrentTenantId);

        builder.Entity<LaytimeCalculation>().HasQueryFilter(lt =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            lt.TenantId == CurrentTenantId);

        builder.Entity<EmailTemplate>().HasQueryFilter(et =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            et.TenantId == CurrentTenantId);

        builder.Entity<EmailDistributionList>().HasQueryFilter(edl =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            edl.TenantId == CurrentTenantId);

        builder.Entity<EnumerationValue>().HasQueryFilter(ev =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            ev.TenantId == CurrentTenantId);

        builder.Entity<CargoMaster>().HasQueryFilter(cm =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            cm.TenantId == CurrentTenantId);

        builder.Entity<AreaConstraint>().HasQueryFilter(ac =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            ac.TenantId == CurrentTenantId);

        builder.Entity<SavedPassage>().HasQueryFilter(sp =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            sp.TenantId == CurrentTenantId);

        builder.Entity<WorkflowConfiguration>().HasQueryFilter(wc =>
            CurrentTenantId == null ||
            CurrentUserRole == MultiTenantSaaS.Domain.Enums.UserRoles.SuperAdmin ||
            wc.TenantId == CurrentTenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedByUserId = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedByUserId = currentUserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
