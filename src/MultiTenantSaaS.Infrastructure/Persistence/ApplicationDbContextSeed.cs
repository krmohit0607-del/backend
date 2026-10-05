using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedDefaultDataAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        try
        {
            // 1. Seed Roles
            foreach (var roleName in UserRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName)
                    {
                        Description = $"System role for {roleName}"
                    });
                    logger.LogInformation("Seeded role: {Role}", roleName);
                }
            }

            // 2. Seed Default Modules
            var defaultModules = new List<Module>
            {
                new()
                {
                    Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    Name = "Chartering",
                    NormalizedName = "CHARTERING",
                    Description = "Voyage estimation, chartering books, and fixture management",
                    IsGlobalActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                    Name = "Operations",
                    NormalizedName = "OPERATIONS",
                    Description = "Voyage execution, reporting, routing, and vessel operations",
                    IsGlobalActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                    Name = "Bunker",
                    NormalizedName = "BUNKER",
                    Description = "Bunker requirements, RFQs, supply, and payments",
                    IsGlobalActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
                    Name = "Postfix",
                    NormalizedName = "POSTFIX",
                    Description = "Voyage settlement, claims, laytime, and accounting",
                    IsGlobalActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new() { Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"), Name = "Emissions", NormalizedName = "EMISSIONS", Description = "Emissions records, adjustments, and reporting", IsGlobalActive = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.Parse("a6666666-6666-6666-6666-666666666666"), Name = "Performance", NormalizedName = "PERFORMANCE", Description = "Vessel performance, weather routing, and optimization", IsGlobalActive = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.Parse("a7777777-7777-7777-7777-777777777777"), Name = "Accounts", NormalizedName = "ACCOUNTS", Description = "Financial transactions, payables, receivables, and cash flow", IsGlobalActive = true, CreatedAt = DateTime.UtcNow }
            };

            var obsoleteModules = await context.Modules.IgnoreQueryFilters()
                .Where(m => new[] { "INVENTORY", "PAYROLL", "REPORTS", "CRM" }.Contains(m.NormalizedName))
                .ToListAsync();
            foreach (var obsolete in obsoleteModules)
            {
                var isReferenced = await context.TenantModuleAccesses.IgnoreQueryFilters().AnyAsync(a => a.ModuleId == obsolete.Id) ||
                    await context.EmployeeModulePermissions.IgnoreQueryFilters().AnyAsync(p => p.ModuleId == obsolete.Id);
                if (!isReferenced) context.Modules.Remove(obsolete);
            }

            foreach (var module in defaultModules)
            {
                if (!await context.Modules.IgnoreQueryFilters().AnyAsync(m => m.NormalizedName == module.NormalizedName))
                {
                    context.Modules.Add(module);
                    logger.LogInformation("Seeded module: {Module}", module.Name);
                }
            }
            await context.SaveChangesAsync();

            // 3. Seed Default SuperAdmin User
            const string superAdminEmail = "superadmin@saas.com";
            const string superAdminPassword = "SuperAdmin123!";
            var defaultSuperAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (defaultSuperAdmin == null)
            {
                var superAdmin = new ApplicationUser
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Global Super Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = null,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(superAdmin, superAdminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);
                    logger.LogInformation("Seeded default SuperAdmin user: {Email}", superAdminEmail);
                }
                else
                {
                    logger.LogError("Failed to seed SuperAdmin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                defaultSuperAdmin.UserName = superAdminEmail;
                defaultSuperAdmin.Email = superAdminEmail;
                defaultSuperAdmin.EmailConfirmed = true;
                defaultSuperAdmin.IsActive = true;
                defaultSuperAdmin.TenantId = null;
                await userManager.UpdateAsync(defaultSuperAdmin);

                if (!await userManager.IsInRoleAsync(defaultSuperAdmin, UserRoles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(defaultSuperAdmin, UserRoles.SuperAdmin);
                }

                if (!await userManager.CheckPasswordAsync(defaultSuperAdmin, superAdminPassword))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(defaultSuperAdmin);
                    var resetResult = await userManager.ResetPasswordAsync(defaultSuperAdmin, resetToken, superAdminPassword);
                    if (!resetResult.Succeeded)
                    {
                        logger.LogError("Failed to repair SuperAdmin password: {Errors}", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 4. Seed Default Test Admin & Tenant (Acme Shipping & Logistics)
            var testTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var testAdminId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            const string testAdminEmail = "admin@acme.com";

            var defaultAdmin = await userManager.FindByEmailAsync(testAdminEmail);
            if (defaultAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    Id = testAdminId,
                    UserName = testAdminEmail,
                    Email = testAdminEmail,
                    FullName = "Alice Johnson (Admin)",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = null,
                    PhoneNumber = "+1 (555) 123-4567",
                    CreatedAt = DateTime.UtcNow
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "AdminPassword123!");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                    logger.LogInformation("Seeded test Admin user: {Email}", testAdminEmail);
                }
                else
                {
                    logger.LogError("Failed to seed Admin user: {Errors}", string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                }
                defaultAdmin = adminUser;
            }

            var defaultTenant = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == testTenantId);
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    Id = testTenantId,
                    CompanyName = "Acme Shipping & Logistics",
                    AdminUserId = testAdminId,
                    IsActive = true,
                    SubscriptionStatus = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.Tenants.Add(defaultTenant);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded test Tenant: {Company}", defaultTenant.CompanyName);
            }

            if (defaultAdmin != null && defaultAdmin.TenantId != testTenantId)
            {
                defaultAdmin.TenantId = testTenantId;
                await userManager.UpdateAsync(defaultAdmin);
            }

            // 5. Grant Default Modules to Test Tenant (Inventory, Reports, CRM)
            var tenantModuleIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"), // Inventory
                Guid.Parse("33333333-3333-3333-3333-333333333333"), // Reports
                Guid.Parse("44444444-4444-4444-4444-444444444444"), // CRM
            };

            foreach (var modId in tenantModuleIds)
            {
                var hasAccess = await context.TenantModuleAccesses
                    .IgnoreQueryFilters()
                    .AnyAsync(t => t.TenantId == testTenantId && t.ModuleId == modId);

                if (!hasAccess)
                {
                    context.TenantModuleAccesses.Add(new TenantModuleAccess
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        ModuleId = modId,
                        IsEnabled = true,
                        GrantedByUserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        GrantedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();

            // 6. Seed Default Test Employee (Bob Martinez @ Acme)
            var testEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            const string testEmployeeEmail = "employee@acme.com";
            var defaultEmployee = await userManager.FindByEmailAsync(testEmployeeEmail);

            if (defaultEmployee == null)
            {
                var empUser = new ApplicationUser
                {
                    Id = testEmployeeId,
                    UserName = testEmployeeEmail,
                    Email = testEmployeeEmail,
                    FullName = "Bob Martinez (Employee)",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = testTenantId,
                    CreatedByUserId = testAdminId,
                    PhoneNumber = "+1 (555) 987-6543",
                    CreatedAt = DateTime.UtcNow
                };

                var createEmpResult = await userManager.CreateAsync(empUser, "EmployeePassword123!");
                if (createEmpResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(empUser, UserRoles.Employee);
                    logger.LogInformation("Seeded test Employee user: {Email}", testEmployeeEmail);
                }
                else
                {
                    logger.LogError("Failed to seed Employee user: {Errors}", string.Join(", ", createEmpResult.Errors.Select(e => e.Description)));
                }

                // Assign employee permissions for Inventory (View, Create, Edit) & Reports (View only)
                var inventoryModId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                var reportsModId = Guid.Parse("33333333-3333-3333-3333-333333333333");

                context.EmployeeModulePermissions.AddRange(
                    new EmployeeModulePermission
                    {
                        Id = Guid.NewGuid(),
                        EmployeeUserId = testEmployeeId,
                        ModuleId = inventoryModId,
                        TenantId = testTenantId,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = false,
                        AssignedByUserId = testAdminId,
                        AssignedAt = DateTime.UtcNow
                    },
                    new EmployeeModulePermission
                    {
                        Id = Guid.NewGuid(),
                        EmployeeUserId = testEmployeeId,
                        ModuleId = reportsModId,
                        TenantId = testTenantId,
                        CanView = true,
                        CanCreate = false,
                        CanEdit = false,
                        CanDelete = false,
                        AssignedByUserId = testAdminId,
                        AssignedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded test Employee permissions for: {Email}", testEmployeeEmail);
            }

            // 7. Seed Default Test Vessel Master (Captain Edward Smith @ MV Oceanic Pioneer)
            var testVesselMasterId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            const string testVesselMasterEmail = "master@oceanic.com";
            var defaultVesselMaster = await userManager.FindByEmailAsync(testVesselMasterEmail);

            if (defaultVesselMaster == null)
            {
                var masterUser = new ApplicationUser
                {
                    Id = testVesselMasterId,
                    UserName = testVesselMasterEmail,
                    Email = testVesselMasterEmail,
                    FullName = "Capt. Edward Smith (Vessel Master)",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = testTenantId,
                    CreatedByUserId = testAdminId,
                    AssignedVesselImo = "9417878",
                    AssignedVesselName = "MV OCEANIC PIONEER",
                    PhoneNumber = "+1 (555) 777-8899",
                    CreatedAt = DateTime.UtcNow
                };

                var createMasterResult = await userManager.CreateAsync(masterUser, "MasterPassword123!");
                if (createMasterResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(masterUser, UserRoles.VesselMaster);
                    logger.LogInformation("Seeded test Vessel Master user: {Email}", testVesselMasterEmail);
                }
                else
                {
                    logger.LogError("Failed to seed Vessel Master user: {Errors}", string.Join(", ", createMasterResult.Errors.Select(e => e.Description)));
                }
            }

            // 8. Do not seed demo voyages; operational records come from the database.
            var demoVesselImos = new[] { "9417878", "9670585", "9321483", "9074729" };
            await context.Voyages
                .IgnoreQueryFilters()
                .Where(v => v.TenantId == testTenantId && demoVesselImos.Contains(v.Imo))
                .ExecuteDeleteAsync();
            if (false && !await context.Voyages.IgnoreQueryFilters().AnyAsync(v => v.TenantId == testTenantId))
            {
                var sampleVoyages = new List<Voyage>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        VoyageCode = "OPT001",
                        VesselName = "MV OCEANIC PIONEER",
                        Imo = "9417878",
                        VesselType = "Bulk Carrier (Capesize)",
                        Flag = "Singapore",
                        PortFrom = "Singapore",
                        PortTo = "Santos",
                        Status = "At Sea",
                        Priority = "HIGH",
                        EtaDisplay = "18-Jun 1200",
                        EtdDisplay = "14 Jun 2026, 04:50",
                        LastNoon = "0600 UTC",
                        Pic = "Capt. Edward Smith",
                        Client = "Cargill",
                        ClientEmail = "ops@cargill.example.com",
                        Service = "PMO",
                        CpSpeed = 13.0,
                        CpCons = 30.0,
                        InstSpeed = 13.5,
                        InstCons = 29.8,
                        Health = 88,
                        Remaining = "01:20",
                        DueLt = 900,
                        DueUtc = 300,
                        OpenTasks = 3,
                        Tags = "Typhoon, ETA",
                        AiAlert = "ETA Risk",
                        HandoverNote = "Monitor weather near Japan",
                        OpenStatus = "OPEN",
                        CostPerDay = 58400,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        VoyageCode = "OPT002",
                        VesselName = "MV ATLANTIC TRADER",
                        Imo = "9321483",
                        VesselType = "Bulk Carrier (Panamax)",
                        Flag = "Panama",
                        PortFrom = "Fujairah",
                        PortTo = "Rotterdam",
                        Status = "At Sea",
                        Priority = "MEDIUM",
                        EtaDisplay = "22-Jun 0800",
                        EtdDisplay = "10 Jun 2026, 14:00",
                        LastNoon = "0500 UTC",
                        Pic = "Rahul",
                        Client = "Bunge",
                        ClientEmail = "chartering@bunge.example.com",
                        Service = "RPM",
                        CpSpeed = 13.0,
                        CpCons = 32.0,
                        InstSpeed = 13.4,
                        InstCons = 33.1,
                        Health = 78,
                        Remaining = "02:35",
                        DueLt = 1000,
                        DueUtc = 400,
                        OpenTasks = 2,
                        Tags = "FuelIssue",
                        AiAlert = "Fuel Increase",
                        HandoverNote = "Awaiting owner reply",
                        OpenStatus = "OPEN",
                        CostPerDay = 61200,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        VoyageCode = "OPT003",
                        VesselName = "MV PACIFIC HORIZON",
                        Imo = "9074729",
                        VesselType = "Oil / Chemical Tanker",
                        Flag = "Marshall Islands",
                        PortFrom = "Santos",
                        PortTo = "Santos",
                        Status = "At Port",
                        Priority = "LOW",
                        EtaDisplay = "N/A",
                        EtdDisplay = "08 Jun 2026, 09:00",
                        LastNoon = "0000 UTC",
                        Pic = "John",
                        Client = "Trafigura",
                        ClientEmail = "ops@trafigura.example.com",
                        Service = "Monitoring",
                        CpSpeed = 11.5,
                        CpCons = 24.0,
                        InstSpeed = 0,
                        InstCons = 0,
                        Health = 98,
                        Remaining = "05:10",
                        DueLt = 1300,
                        DueUtc = 700,
                        OpenTasks = 0,
                        Tags = "PortStay",
                        AiAlert = "None",
                        HandoverNote = "Cargo ops ongoing",
                        OpenStatus = "OPEN",
                        CostPerDay = 42000,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Voyages.AddRange(sampleVoyages);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test Voyages for Tenant {Tenant}", sampleVoyages.Count, testTenantId);
            }

            var removedDemoVessels = await context.Vessels
                .IgnoreQueryFilters()
                .Where(v => v.TenantId == testTenantId && demoVesselImos.Contains(v.Imo))
                .ExecuteDeleteAsync();
            if (removedDemoVessels > 0)
            {
                logger.LogInformation("Removed {Count} seeded demo vessels for Tenant {Tenant}", removedDemoVessels, testTenantId);
            }

            var demoVesselNames = new[] { "MV OCEANIC PIONEER", "MV ATLANTIC TRADER", "MV PACIFIC HORIZON", "ATLANTIC SAIL" };
            await context.VoyageEstimates
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == testTenantId && demoVesselNames.Contains(e.VesselName))
                .ExecuteDeleteAsync();
            await context.BunkerRequirements
                .IgnoreQueryFilters()
                .Where(b => b.TenantId == testTenantId && (demoVesselNames.Contains(b.VesselName) || demoVesselImos.Contains(b.Imo)))
                .ExecuteDeleteAsync();

            // 10. Seed Default Voyage Estimates & Books for Acme Tenant
            if (false && !await context.VoyageEstimates.IgnoreQueryFilters().AnyAsync(e => e.TenantId == testTenantId))
            {
                var sampleEstimates = new List<VoyageEstimate>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        EstimateNo = "EST-2608-01",
                        VesselName = "MV OCEANIC PIONEER",
                        FixType = "Voyage Charter",
                        Status = "Fixed",
                        Profit = 142500,
                        Tce = 23800,
                        Commodity = "Iron Ore",
                        LoadPort = "Port Hedland",
                        DischargePort = "Qingdao",
                        Quantity = 170000,
                        FreightRate = 12.85,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        EstimateNo = "EST-2608-02",
                        VesselName = "MV ATLANTIC TRADER",
                        FixType = "Voyage Charter",
                        Status = "Draft",
                        Profit = 68400,
                        Tce = 18450,
                        Commodity = "Steam Coal",
                        LoadPort = "Richards Bay",
                        DischargePort = "Paradip",
                        Quantity = 75000,
                        FreightRate = 16.50,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                context.VoyageEstimates.AddRange(sampleEstimates);

                var sampleCargos = new List<CargoBookEntry>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        CargoCode = "CG-2608-001",
                        Commodity = "Iron Ore",
                        CargoType = "Bulk",
                        Quantity = "170,000",
                        Tolerance = "±10%",
                        LoadPort = "Port Hedland",
                        DischargePort = "Qingdao",
                        LoadRate = "90,000 MT/day",
                        DischargeRate = "70,000 MT/day",
                        Terms = "FIOST",
                        LaycanStart = "2026-08-28",
                        LaycanEnd = "2026-09-03",
                        VoyageType = "Voyage Charter",
                        OpenDate = "2026-08-18",
                        NominationDeadline = "2026-08-24",
                        CargoStatus = "Open",
                        CommercialStatus = "Reviewing",
                        Pic = "Amit",
                        EstimationStatus = "Not Created",
                        Account = "Cargill",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        CargoCode = "CG-2608-002",
                        Commodity = "Steam Coal",
                        CargoType = "Bulk",
                        Quantity = "75,000",
                        Tolerance = "±5%",
                        LoadPort = "Richards Bay",
                        DischargePort = "Paradip",
                        LoadRate = "45,000 MT/day",
                        DischargeRate = "35,000 MT/day",
                        Terms = "FIO",
                        LaycanStart = "2026-09-04",
                        LaycanEnd = "2026-09-10",
                        VoyageType = "Voyage Charter",
                        OpenDate = "2026-08-15",
                        NominationDeadline = "2026-08-27",
                        CargoStatus = "Offered",
                        CommercialStatus = "Offered",
                        Pic = "Rahul",
                        EstimationStatus = "Draft",
                        Account = "Bunge",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                context.CargoBookEntries.AddRange(sampleCargos);

                var sampleTonnage = new List<TonnageBookEntry>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        TonnageCode = "TN-2608-001",
                        VesselName = "MV OCEANIC PIONEER",
                        Imo = "9811000",
                        VesselType = "Bulk Carrier",
                        Dwt = "180,000",
                        Flag = "Singapore",
                        OpenArea = "SE Asia",
                        OpenPort = "Singapore",
                        OpenDate = "2026-08-25",
                        EarliestOpen = "2026-08-24",
                        LatestOpen = "2026-08-29",
                        VoyageType = "Time Charter",
                        Source = "Own",
                        CommercialStatus = "Open",
                        Pic = "Amit",
                        EstimationStatus = "Estimated",
                        Owner = "Acme Shipping & Logistics",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        TonnageCode = "TN-2608-002",
                        VesselName = "MV PACIFIC HORIZON",
                        Imo = "9633441",
                        VesselType = "Kamsarmax",
                        Dwt = "82,000",
                        Flag = "Marshall Islands",
                        OpenArea = "Australia",
                        OpenPort = "Newcastle",
                        OpenDate = "2026-09-02",
                        EarliestOpen = "2026-09-01",
                        LatestOpen = "2026-09-06",
                        VoyageType = "Voyage Charter",
                        Source = "Broker",
                        CommercialStatus = "On Subs",
                        Pic = "Rahul",
                        EstimationStatus = "Draft",
                        Owner = "Ocean Brokers",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                context.TonnageBookEntries.AddRange(sampleTonnage);

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Chartering & Estimates data for Tenant {Tenant}", testTenantId);
            }

            // 11. Seed Default Bunker Requirements for Acme Tenant
            if (false && !await context.BunkerRequirements.IgnoreQueryFilters().AnyAsync(b => b.TenantId == testTenantId))
            {
                var sampleBunkers = new List<BunkerRequirement>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        RequirementNo = "BR-2606-024",
                        Priority = "High",
                        Status = "Pending RFQ",
                        VesselName = "MV OCEANIC PIONEER",
                        Imo = "9417878",
                        Leg = "LEG-1",
                        Route = "Singapore → Cape Town",
                        LoadPort = "Singapore",
                        DischargePort = "Cape Town",
                        BunkerPort = "Singapore",
                        Eta = "14 Jun 2026, 06:00 LT",
                        RequiredOn = "12 Jun 2026, 10:00 LT",
                        RequiredIso = "2026-06-12T10:00",
                        FuelType = "VLSFO",
                        Grade = "ISO 8217:2017 RMG 380",
                        Quantity = 1500,
                        RobArrival = 220,
                        ExpectedCons = 60,
                        ChartererInstructions = "Bunker at Singapore OPL.",
                        OwnerInstructions = "Max sulphur 0.50%. Density max at 15°C: 991.",
                        SuppliersInvited = 6,
                        Supplier = "Ocean Bunkers",
                        PricePerMt = 680,
                        TotalCost = 1020000,
                        ApprovalStatus = "Not Submitted",
                        PaymentStatus = "None",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        RequirementNo = "BR-2606-021",
                        Priority = "High",
                        Status = "Booked",
                        VesselName = "MV ATLANTIC TRADER",
                        Imo = "9321483",
                        Leg = "LEG-2",
                        Route = "Australia → India",
                        LoadPort = "Port Hedland",
                        DischargePort = "Visakhapatnam",
                        BunkerPort = "Durban, South Africa",
                        Eta = "18 Jun 2026, 08:30 LT",
                        RequiredOn = "17 Jun 2026, 09:00 LT",
                        RequiredIso = "2026-06-17T09:00",
                        FuelType = "VLSFO",
                        Grade = "ISO 8217:2017 RMG 380",
                        Quantity = 1200,
                        RobArrival = 380,
                        ExpectedCons = 55,
                        ChartererInstructions = "As per charterers instruction.",
                        OwnerInstructions = "BDN + sample required.",
                        SuppliersInvited = 5,
                        Supplier = "Ocean Bunkers",
                        PricePerMt = 690,
                        TotalCost = 828000,
                        PoNo = "PO-2606-122",
                        ContractRef = "CON-OB-06-08",
                        BookedOn = "14 Jun 2026, 16:20",
                        ConfirmNo = "OB-SIN-67890",
                        DeliveryMethod = "Truck to Vessel",
                        ApprovalStatus = "Not Submitted",
                        PaymentStatus = "None",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        TenantId = testTenantId,
                        RequirementNo = "BR-2606-019",
                        Priority = "Medium",
                        Status = "Payment Due",
                        VesselName = "MV PACIFIC HORIZON",
                        Imo = "9074729",
                        Leg = "LEG-2",
                        Route = "SE Asia → Korea",
                        LoadPort = "Jakarta",
                        DischargePort = "Busan",
                        BunkerPort = "Singapore",
                        Eta = "20 Jun 2026, 05:00 LT",
                        RequiredOn = "19 Jun 2026, 14:00 LT",
                        RequiredIso = "2026-06-19T14:00",
                        FuelType = "MGO",
                        Grade = "ISO 8217:2017 DMA",
                        Quantity = 150,
                        RobArrival = 30,
                        ExpectedCons = 7,
                        ChartererInstructions = "ECA compliant MGO.",
                        OwnerInstructions = "Sample retained 12 months.",
                        SuppliersInvited = 5,
                        Supplier = "Ocean Bunkers",
                        PricePerMt = 630,
                        TotalCost = 94500,
                        PoNo = "PO-2606-110",
                        ContractRef = "CON-OB-06-01",
                        BookedOn = "08 Jun 2026, 09:15",
                        ConfirmNo = "OB-SIN-66120",
                        DeliveryMethod = "Barge",
                        SuppliedQty = 150,
                        DeliveredQty = 150,
                        InvoiceNo = "INV-OB-66120",
                        InvoiceDate = "13 Jun 2026",
                        InvoiceAmount = 94500,
                        PaymentTerms = "15 days from BDN",
                        DueDate = "28 Jun 2026",
                        DueIso = "2026-06-28",
                        AmountPaid = 0,
                        ApprovalStatus = "Approved",
                        PaymentStatus = "Due in 7 Days",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                context.BunkerRequirements.AddRange(sampleBunkers);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Bunker Requirements for Tenant {Tenant}", testTenantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding database.");
            throw;
        }
    }
}
