using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Module> Modules { get; }
    DbSet<TenantModuleAccess> TenantModuleAccesses { get; }
    DbSet<EmployeeModulePermission> EmployeeModulePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<VoyageOrder> VoyageOrders { get; }
    DbSet<Voyage> Voyages { get; }
    DbSet<Passage> Passages { get; }
    DbSet<PassageLeg> PassageLegs { get; }
    DbSet<Vessel> Vessels { get; }
    DbSet<VesselHistory> VesselHistories { get; }
    DbSet<VoyageEstimate> VoyageEstimates { get; }
    DbSet<CargoBookEntry> CargoBookEntries { get; }
    DbSet<TonnageBookEntry> TonnageBookEntries { get; }
    DbSet<BunkerRequirement> BunkerRequirements { get; }
    DbSet<EmissionsRecord> EmissionsRecords { get; }
    DbSet<EmissionsScenario> EmissionsScenarios { get; }
    DbSet<FinancialTransaction> FinancialTransactions { get; }
    DbSet<VesselReport> VesselReports { get; }
    DbSet<UserSetting> UserSettings { get; }
    DbSet<ImoShip> ImoShips { get; }
    DbSet<Port> Ports { get; }

    // Tier 1 & 2 Critical Entities
    DbSet<VoyageRecap> VoyageRecaps { get; }
    DbSet<ProFormaDisbursement> ProFormaDisbursements { get; }
    DbSet<FinalDisbursement> FinalDisbursements { get; }
    DbSet<AgentInvoice> AgentInvoices { get; }
    DbSet<AdditionalService> AdditionalServices { get; }
    DbSet<ClaimRecord> ClaimRecords { get; }
    DbSet<HirePayment> HirePayments { get; }
    DbSet<SettlementMilestone> SettlementMilestones { get; }
    DbSet<Client> Clients { get; }
    DbSet<ClientContact> ClientContacts { get; }
    DbSet<LaytimeCalculation> LaytimeCalculations { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailDistributionList> EmailDistributionLists { get; }
    DbSet<EnumerationValue> EnumerationValues { get; }
    DbSet<CargoMaster> CargoMasters { get; }
    DbSet<AreaConstraint> AreaConstraints { get; }
    DbSet<SavedPassage> SavedPassages { get; }
    DbSet<WorkflowConfiguration> WorkflowConfigurations { get; }
    DbSet<WeatherGridSnapshot> WeatherGridSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
