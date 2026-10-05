using Microsoft.AspNetCore.Identity;

namespace MultiTenantSaaS.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }
}
