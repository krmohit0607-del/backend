using AutoMapper;
using MultiTenantSaaS.Application.DTOs.Admin;
using MultiTenantSaaS.Application.DTOs.Auth;
using MultiTenantSaaS.Application.DTOs.Employee;
using MultiTenantSaaS.Application.DTOs.SuperAdmin;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth / User
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Tenant != null ? s.Tenant.CompanyName : null));

        // SuperAdmin
        CreateMap<Tenant, TenantDto>()
            .ForMember(d => d.AdminFullName, opt => opt.MapFrom(s => s.AdminUser != null ? s.AdminUser.FullName : string.Empty))
            .ForMember(d => d.AdminEmail, opt => opt.MapFrom(s => s.AdminUser != null ? s.AdminUser.Email : string.Empty))
            .ForMember(d => d.ActiveUsersCount, opt => opt.MapFrom(s => s.Users.Count(u => u.IsActive)))
            .ForMember(d => d.EnabledModulesCount, opt => opt.MapFrom(s => s.TenantModuleAccesses.Count(t => t.IsEnabled && t.Module != null && t.Module.IsGlobalActive)));

        CreateMap<Module, ModuleDto>();

        CreateMap<TenantModuleAccess, TenantModuleAccessDto>()
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : string.Empty))
            .ForMember(d => d.ModuleDescription, opt => opt.MapFrom(s => s.Module != null ? s.Module.Description : null))
            .ForMember(d => d.IsGlobalActive, opt => opt.MapFrom(s => s.Module != null && s.Module.IsGlobalActive))
            .ForMember(d => d.GrantedByUserName, opt => opt.MapFrom(s => s.GrantedByUser != null ? s.GrantedByUser.FullName : string.Empty));

        // Admin / Employee
        CreateMap<ApplicationUser, EmployeeDto>()
            .ForMember(d => d.Permissions, opt => opt.MapFrom(s => s.ModulePermissions));

        CreateMap<EmployeeModulePermission, EmployeePermissionDto>()
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : string.Empty));

        CreateMap<EmployeeModulePermission, EmployeeModuleAccessDto>()
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : string.Empty))
            .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Module != null ? s.Module.Description : null));

        // Module 1: Fleet & Voyages
        CreateMap<Voyage, MultiTenantSaaS.Application.DTOs.Voyages.VoyageDto>();
        CreateMap<VoyageOrder, MultiTenantSaaS.Application.DTOs.Voyages.VoyageOrderDto>();
        CreateMap<Passage, MultiTenantSaaS.Application.DTOs.Voyages.PassageDto>();
        CreateMap<PassageLeg, MultiTenantSaaS.Application.DTOs.Voyages.PassageLegDto>();

        // Module 2: Vessels & Master Registry
        CreateMap<Vessel, MultiTenantSaaS.Application.DTOs.Vessels.VesselDto>();
        CreateMap<VesselHistory, MultiTenantSaaS.Application.DTOs.Vessels.VesselHistoryDto>();

        // Module 3: Chartering & Voyage Estimation
        CreateMap<VoyageEstimate, MultiTenantSaaS.Application.DTOs.Chartering.VoyageEstimateDto>();
        CreateMap<CargoBookEntry, MultiTenantSaaS.Application.DTOs.Chartering.CargoBookDto>();
        CreateMap<TonnageBookEntry, MultiTenantSaaS.Application.DTOs.Chartering.TonnageBookDto>();

        // Module 4: Bunker Management & Fuel Inventory
        CreateMap<BunkerRequirement, MultiTenantSaaS.Application.DTOs.Bunker.BunkerRequirementDto>();

        // Module 5: Emissions & Compliance
        CreateMap<EmissionsRecord, MultiTenantSaaS.Application.DTOs.Emissions.EmissionsRecordDto>();
        CreateMap<EmissionsScenario, MultiTenantSaaS.Application.DTOs.Emissions.EmissionsScenarioDto>();

        // Module 6: Accounts & Invoicing
        CreateMap<FinancialTransaction, MultiTenantSaaS.Application.DTOs.Accounts.FinancialTransactionDto>();

        // Module 7: Vessel Reporting & Telemetry
        CreateMap<VesselReport, MultiTenantSaaS.Application.DTOs.VesselReports.VesselReportDto>();
    }
}
