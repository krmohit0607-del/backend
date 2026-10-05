namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action, 
        string entityName, 
        string? entityId = null, 
        string? oldValues = null, 
        string? newValues = null, 
        Guid? tenantId = null, 
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
