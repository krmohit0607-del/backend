namespace MultiTenantSaaS.Domain.Common;

public interface IHasTenant
{
    Guid? TenantId { get; set; }
}
