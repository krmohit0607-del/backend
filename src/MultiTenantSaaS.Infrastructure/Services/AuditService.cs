using Microsoft.AspNetCore.Http;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        Guid? tenantId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IPAddress = ip,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
