namespace MultiTenantSaaS.Domain.Enums;

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string VesselMaster = "VesselMaster";

    public static readonly IReadOnlyList<string> All = new[]
    {
        SuperAdmin,
        Admin,
        Employee,
        VesselMaster
    };
}
