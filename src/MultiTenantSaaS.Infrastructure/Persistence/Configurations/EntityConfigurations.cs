using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CompanyName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.SubscriptionStatus)
            .IsRequired();

        builder.HasOne(t => t.AdminUser)
            .WithMany()
            .HasForeignKey(t => t.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.TenantModuleAccesses)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.CreatedByUser)
            .WithMany(u => u.CreatedUsers)
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ModulePermissions)
            .WithOne(p => p.EmployeeUser)
            .HasForeignKey(p => p.EmployeeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("Modules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.NormalizedName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(m => m.NormalizedName)
            .IsUnique();

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.HasMany(m => m.TenantModuleAccesses)
            .WithOne(t => t.Module)
            .HasForeignKey(t => t.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.EmployeePermissions)
            .WithOne(p => p.Module)
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TenantModuleAccessConfiguration : IEntityTypeConfiguration<TenantModuleAccess>
{
    public void Configure(EntityTypeBuilder<TenantModuleAccess> builder)
    {
        builder.ToTable("TenantModuleAccesses");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.TenantId, t.ModuleId })
            .IsUnique();

        builder.HasOne(t => t.Tenant)
            .WithMany(tenant => tenant.TenantModuleAccesses)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Module)
            .WithMany(m => m.TenantModuleAccesses)
            .HasForeignKey(t => t.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.GrantedByUser)
            .WithMany()
            .HasForeignKey(t => t.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeModulePermissionConfiguration : IEntityTypeConfiguration<EmployeeModulePermission>
{
    public void Configure(EntityTypeBuilder<EmployeeModulePermission> builder)
    {
        builder.ToTable("EmployeeModulePermissions");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.EmployeeUserId, p.ModuleId })
            .IsUnique();

        builder.HasOne(p => p.EmployeeUser)
            .WithMany(u => u.ModulePermissions)
            .HasForeignKey(p => p.EmployeeUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Module)
            .WithMany(m => m.EmployeePermissions)
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AssignedByUser)
            .WithMany()
            .HasForeignKey(p => p.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(r => r.Token)
            .IsUnique();

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .HasMaxLength(100);

        builder.Property(a => a.IPAddress)
            .HasMaxLength(50);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class VoyageOrderConfiguration : IEntityTypeConfiguration<VoyageOrder>
{
    public void Configure(EntityTypeBuilder<VoyageOrder> builder)
    {
        builder.ToTable("VoyageOrders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Client)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(o => o.Tenant)
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Voyages)
            .WithOne(v => v.VoyageOrder)
            .HasForeignKey(v => v.VoyageOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class VoyageConfiguration : IEntityTypeConfiguration<Voyage>
{
    public void Configure(EntityTypeBuilder<Voyage> builder)
    {
        builder.ToTable("Voyages");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VoyageCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.VesselName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(v => v.Tenant)
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Passages)
            .WithOne(p => p.Voyage)
            .HasForeignKey(p => p.VoyageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PassageConfiguration : IEntityTypeConfiguration<Passage>
{
    public void Configure(EntityTypeBuilder<Passage> builder)
    {
        builder.ToTable("Passages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Legs)
            .WithOne(l => l.Passage)
            .HasForeignKey(l => l.PassageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PassageLegConfiguration : IEntityTypeConfiguration<PassageLeg>
{
    public void Configure(EntityTypeBuilder<PassageLeg> builder)
    {
        builder.ToTable("PassageLegs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.FromPort)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.ToPort)
            .IsRequired()
            .HasMaxLength(100);
    }
}

public class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("Vessels");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(v => v.Imo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(v => new { v.TenantId, v.Imo });

        builder.HasOne(v => v.Tenant)
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.History)
            .WithOne(h => h.Vessel)
            .HasForeignKey(h => h.VesselId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VesselHistoryConfiguration : IEntityTypeConfiguration<VesselHistory>
{
    public void Configure(EntityTypeBuilder<VesselHistory> builder)
    {
        builder.ToTable("VesselHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.FieldName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.ChangedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(h => h.Tenant)
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserSettingConfiguration : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
        builder.ToTable("UserSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).IsRequired().HasMaxLength(150);
        builder.Property(s => s.ValueJson).IsRequired();
        builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
        builder.HasOne(s => s.Tenant).WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.UpdatedByUser).WithMany().HasForeignKey(s => s.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class VoyageEstimateConfiguration : IEntityTypeConfiguration<VoyageEstimate>
{
    public void Configure(EntityTypeBuilder<VoyageEstimate> builder)
    {
        builder.ToTable("VoyageEstimates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EstimateNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CargoBookEntryConfiguration : IEntityTypeConfiguration<CargoBookEntry>
{
    public void Configure(EntityTypeBuilder<CargoBookEntry> builder)
    {
        builder.ToTable("CargoBookEntries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CargoCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Commodity)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TonnageBookEntryConfiguration : IEntityTypeConfiguration<TonnageBookEntry>
{
    public void Configure(EntityTypeBuilder<TonnageBookEntry> builder)
    {
        builder.ToTable("TonnageBookEntries");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TonnageCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BunkerRequirementConfiguration : IEntityTypeConfiguration<BunkerRequirement>
{
    public void Configure(EntityTypeBuilder<BunkerRequirement> builder)
    {
        builder.ToTable("BunkerRequirements");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.RequirementNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.BunkerPort)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(b => b.Tenant)
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmissionsRecordConfiguration : IEntityTypeConfiguration<EmissionsRecord>
{
    public void Configure(EntityTypeBuilder<EmissionsRecord> builder)
    {
        builder.ToTable("EmissionsRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.VoyageCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmissionsScenarioConfiguration : IEntityTypeConfiguration<EmissionsScenario>
{
    public void Configure(EntityTypeBuilder<EmissionsScenario> builder)
    {
        builder.ToTable("EmissionsScenarios");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.VoyageCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("FinancialTransactions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.TransactionNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(f => f.Counterparty)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(f => f.Tenant)
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VesselReportConfiguration : IEntityTypeConfiguration<VesselReport>
{
    public void Configure(EntityTypeBuilder<VesselReport> builder)
    {
        builder.ToTable("VesselReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.VesselName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Imo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImoShipConfiguration : IEntityTypeConfiguration<ImoShip>
{
    public void Configure(EntityTypeBuilder<ImoShip> builder)
    {
        builder.ToTable("ImoShips");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Imo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.VesselType)
            .HasMaxLength(100);

        builder.Property(i => i.Statcode5)
            .HasMaxLength(50);

        builder.Property(i => i.BuiltYear)
            .HasMaxLength(4);

        builder.HasIndex(i => i.Imo)
            .IsUnique();

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PortConfiguration : IEntityTypeConfiguration<Port>
{
    public void Configure(EntityTypeBuilder<Port> builder)
    {
        builder.ToTable("Ports");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PortName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.PortCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.UnLocode)
            .HasMaxLength(10);

        builder.Property(p => p.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Region)
            .HasMaxLength(100);

        builder.Property(p => p.PortType)
            .HasMaxLength(50);

        builder.HasIndex(p => p.PortCode)
            .IsUnique();

        builder.HasIndex(p => new { p.PortName, p.Country });

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VoyageRecapConfiguration : IEntityTypeConfiguration<VoyageRecap>
{
    public void Configure(EntityTypeBuilder<VoyageRecap> builder)
    {
        builder.ToTable("VoyageRecaps");
        builder.HasKey(vr => vr.Id);
        builder.Property(vr => vr.PdaNo).HasMaxLength(50);
        builder.Property(vr => vr.CpReference).HasMaxLength(100);
        builder.HasOne(vr => vr.Voyage).WithMany().HasForeignKey(vr => vr.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(vr => vr.VoyageId);
    }
}

public class ProFormaDisbursementConfiguration : IEntityTypeConfiguration<ProFormaDisbursement>
{
    public void Configure(EntityTypeBuilder<ProFormaDisbursement> builder)
    {
        builder.ToTable("ProFormaDisbursements");
        builder.HasKey(pda => pda.Id);
        builder.Property(pda => pda.PdaNo).IsRequired().HasMaxLength(50);
        builder.Property(pda => pda.Port).IsRequired().HasMaxLength(100);
        builder.Property(pda => pda.Status).HasMaxLength(50);
        builder.HasOne(pda => pda.Voyage).WithMany().HasForeignKey(pda => pda.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(pda => pda.InvoiceItems).WithOne(ai => ai.Pda).HasForeignKey(ai => ai.PdaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(pda => pda.VoyageId);
    }
}

public class FinalDisbursementConfiguration : IEntityTypeConfiguration<FinalDisbursement>
{
    public void Configure(EntityTypeBuilder<FinalDisbursement> builder)
    {
        builder.ToTable("FinalDisbursements");
        builder.HasKey(fda => fda.Id);
        builder.Property(fda => fda.FdaNo).IsRequired().HasMaxLength(50);
        builder.Property(fda => fda.Port).IsRequired().HasMaxLength(100);
        builder.Property(fda => fda.Status).HasMaxLength(50);
        builder.HasOne(fda => fda.Voyage).WithMany().HasForeignKey(fda => fda.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(fda => fda.VoyageId);
    }
}

public class AgentInvoiceConfiguration : IEntityTypeConfiguration<AgentInvoice>
{
    public void Configure(EntityTypeBuilder<AgentInvoice> builder)
    {
        builder.ToTable("AgentInvoices");
        builder.HasKey(ai => ai.Id);
        builder.Property(ai => ai.InvoiceNo).IsRequired().HasMaxLength(100);
        builder.Property(ai => ai.Agent).IsRequired().HasMaxLength(200);
        builder.Property(ai => ai.Category).IsRequired().HasMaxLength(100);
        builder.Property(ai => ai.DeptStatus).HasMaxLength(50);
        builder.Property(ai => ai.AccountsStatus).HasMaxLength(50);
        builder.HasOne(ai => ai.Voyage).WithMany().HasForeignKey(ai => ai.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ai => ai.Pda).WithMany(pda => pda.InvoiceItems).HasForeignKey(ai => ai.PdaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(ai => ai.VoyageId);
    }
}

public class AdditionalServiceConfiguration : IEntityTypeConfiguration<AdditionalService>
{
    public void Configure(EntityTypeBuilder<AdditionalService> builder)
    {
        builder.ToTable("AdditionalServices");
        builder.HasKey(svc => svc.Id);
        builder.Property(svc => svc.Service).IsRequired().HasMaxLength(200);
        builder.Property(svc => svc.Status).HasMaxLength(50);
        builder.HasOne(svc => svc.Voyage).WithMany().HasForeignKey(svc => svc.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(svc => svc.VoyageId);
    }
}

public class ClaimRecordConfiguration : IEntityTypeConfiguration<ClaimRecord>
{
    public void Configure(EntityTypeBuilder<ClaimRecord> builder)
    {
        builder.ToTable("ClaimRecords");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.VoyageId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ClaimKey).IsRequired().HasMaxLength(100);
        builder.Property(c => c.VesselName).HasMaxLength(200);
        builder.Property(c => c.ClaimType).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ClaimReference).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Status).HasMaxLength(50);
        builder.Property(c => c.PaymentStatus).HasMaxLength(50);
        builder.Property(c => c.WorkflowStatus).HasMaxLength(50);
        builder.HasIndex(c => c.VoyageId);
        builder.HasIndex(c => new { c.VoyageId, c.ClaimKey }).IsUnique();
    }
}

public class HirePaymentConfiguration : IEntityTypeConfiguration<HirePayment>
{
    public void Configure(EntityTypeBuilder<HirePayment> builder)
    {
        builder.ToTable("HirePayments");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.VoyageId).IsRequired().HasMaxLength(100);
        builder.Property(h => h.VesselName).HasMaxLength(200);
        builder.Property(h => h.Side).IsRequired().HasMaxLength(20);
        builder.Property(h => h.InstallmentKey).IsRequired().HasMaxLength(100);
        builder.Property(h => h.Name).HasMaxLength(200);
        builder.Property(h => h.Account).HasMaxLength(200);
        builder.Property(h => h.Status).HasMaxLength(50);
        builder.HasIndex(h => h.VoyageId);
        builder.HasIndex(h => new { h.VoyageId, h.Side, h.InstallmentKey }).IsUnique();
    }
}

public class SettlementMilestoneConfiguration : IEntityTypeConfiguration<SettlementMilestone>
{
    public void Configure(EntityTypeBuilder<SettlementMilestone> builder)
    {
        builder.ToTable("SettlementMilestones");
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.MilestoneLabel).IsRequired().HasMaxLength(200);
        builder.Property(sm => sm.Status).HasMaxLength(50);
        builder.HasOne(sm => sm.Voyage).WithMany().HasForeignKey(sm => sm.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(sm => new { sm.VoyageId, sm.Sequence });
    }
}

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(cl => cl.Id);
        builder.Property(cl => cl.Name).IsRequired().HasMaxLength(300);
        builder.Property(cl => cl.Kind).IsRequired().HasMaxLength(50);
        builder.Property(cl => cl.Category).IsRequired().HasMaxLength(100);
        builder.Property(cl => cl.Email).HasMaxLength(256);
        builder.Property(cl => cl.Username).HasMaxLength(100);
        builder.HasMany(cl => cl.Contacts).WithOne(cc => cc.Client).HasForeignKey(cc => cc.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(cl => new { cl.TenantId, cl.Name });
        builder.HasIndex(cl => cl.Email);
    }
}

public class ClientContactConfiguration : IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> builder)
    {
        builder.ToTable("ClientContacts");
        builder.HasKey(cc => cc.Id);
        builder.Property(cc => cc.Name).IsRequired().HasMaxLength(200);
        builder.Property(cc => cc.Email).HasMaxLength(256);
        builder.HasOne(cc => cc.Client).WithMany(cl => cl.Contacts).HasForeignKey(cc => cc.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(cc => cc.ClientId);
    }
}

public class LaytimeCalculationConfiguration : IEntityTypeConfiguration<LaytimeCalculation>
{
    public void Configure(EntityTypeBuilder<LaytimeCalculation> builder)
    {
        builder.ToTable("LaytimeCalculations");
        builder.HasKey(lt => lt.Id);
        builder.Property(lt => lt.LaytimeTerms).HasMaxLength(100);
        builder.Property(lt => lt.Status).HasMaxLength(50);
        builder.HasOne(lt => lt.Voyage).WithMany().HasForeignKey(lt => lt.VoyageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(lt => lt.VoyageId).IsUnique();
    }
}

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");
        builder.HasKey(et => et.Id);
        builder.Property(et => et.Name).IsRequired().HasMaxLength(300);
        builder.Property(et => et.Category).IsRequired().HasMaxLength(100);
        builder.Property(et => et.Subject).IsRequired().HasMaxLength(500);
        builder.Property(et => et.BodyHtml).IsRequired();
        builder.HasMany(et => et.DistributionLists).WithMany(edl => edl.EmailTemplates).UsingEntity("EmailTemplateDistributionLists");
        builder.HasIndex(et => new { et.TenantId, et.Name });
    }
}

public class EmailDistributionListConfiguration : IEntityTypeConfiguration<EmailDistributionList>
{
    public void Configure(EntityTypeBuilder<EmailDistributionList> builder)
    {
        builder.ToTable("EmailDistributionLists");
        builder.HasKey(edl => edl.Id);
        builder.Property(edl => edl.Name).IsRequired().HasMaxLength(300);
        builder.HasMany(edl => edl.EmailTemplates).WithMany(et => et.DistributionLists).UsingEntity("EmailTemplateDistributionLists");
        builder.HasIndex(edl => new { edl.TenantId, edl.Name });
    }
}

public class EnumerationValueConfiguration : IEntityTypeConfiguration<EnumerationValue>
{
    public void Configure(EntityTypeBuilder<EnumerationValue> builder)
    {
        builder.ToTable("EnumerationValues");
        builder.HasKey(ev => ev.Id);
        builder.Property(ev => ev.EnumerationType).IsRequired().HasMaxLength(100);
        builder.Property(ev => ev.EnumKey).IsRequired().HasMaxLength(100);
        builder.Property(ev => ev.EnumValue).IsRequired().HasMaxLength(300);
        builder.HasIndex(ev => new { ev.TenantId, ev.EnumerationType, ev.EnumKey }).IsUnique();
        builder.HasIndex(ev => new { ev.EnumerationType, ev.IsActive });
    }
}

public class CargoMasterConfiguration : IEntityTypeConfiguration<CargoMaster>
{
    public void Configure(EntityTypeBuilder<CargoMaster> builder)
    {
        builder.ToTable("CargoMasters");
        builder.HasKey(cm => cm.Id);
        builder.Property(cm => cm.CargoCode).IsRequired().HasMaxLength(50);
        builder.Property(cm => cm.CargoName).IsRequired().HasMaxLength(300);
        builder.Property(cm => cm.Category).HasMaxLength(100);
        builder.HasIndex(cm => new { cm.TenantId, cm.CargoCode }).IsUnique();
        builder.HasIndex(cm => new { cm.TenantId, cm.CargoName });
    }
}

public class AreaConstraintConfiguration : IEntityTypeConfiguration<AreaConstraint>
{
    public void Configure(EntityTypeBuilder<AreaConstraint> builder)
    {
        builder.ToTable("AreaConstraints");
        builder.HasKey(ac => ac.Id);
        builder.Property(ac => ac.Name).IsRequired().HasMaxLength(300);
        builder.Property(ac => ac.ConstraintType).IsRequired().HasMaxLength(100);
        builder.Property(ac => ac.GeoJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(ac => new { ac.TenantId, ac.ConstraintType });
    }
}

public class SavedPassageConfiguration : IEntityTypeConfiguration<SavedPassage>
{
    public void Configure(EntityTypeBuilder<SavedPassage> builder)
    {
        builder.ToTable("SavedPassages");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Name).IsRequired().HasMaxLength(300);
        builder.Property(sp => sp.RouteJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(sp => new { sp.TenantId, sp.Name });
    }
}

public class WorkflowConfigurationConfiguration : IEntityTypeConfiguration<WorkflowConfiguration>
{
    public void Configure(EntityTypeBuilder<WorkflowConfiguration> builder)
    {
        builder.ToTable("WorkflowConfigurations");
        builder.HasKey(wc => wc.Id);
        builder.Property(wc => wc.ConfigKey).IsRequired().HasMaxLength(200);
        builder.Property(wc => wc.ConfigValue).IsRequired();
        builder.HasIndex(wc => new { wc.TenantId, wc.ConfigKey }).IsUnique();
    }
}

public class WeatherGridSnapshotConfiguration : IEntityTypeConfiguration<WeatherGridSnapshot>
{
    public void Configure(EntityTypeBuilder<WeatherGridSnapshot> builder)
    {
        builder.ToTable("WeatherGridSnapshots");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.FactorId).IsRequired().HasMaxLength(32);
        builder.Property(w => w.MagnitudeData).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(w => w.DirectionData).HasColumnType("nvarchar(max)");
        // One row per (factor, hour, tile) — the cache key the service looks up by.
        builder.HasIndex(w => new { w.FactorId, w.TimestampUtc, w.TileSouth, w.TileWest }).IsUnique();
    }
}

