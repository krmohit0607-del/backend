using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("userId");

            return Guid.TryParse(idClaim, out var guid) ? guid : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId");
            var result = Guid.TryParse(tenantClaim, out var guid) ? (Guid?)guid : null;
            var email = _httpContextAccessor.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? "unknown";
            System.Console.WriteLine($"[DEBUG] CurrentUserService.TenantId - Email: {email}, tenantClaim: {tenantClaim ?? "NULL"}, Result: {result?.ToString() ?? "NULL"}");
            return result;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
