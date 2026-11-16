namespace MultiTenantSaaS.Infrastructure.Identity;

/// <summary>Carries the tenant/user of a background job (no HTTP context) so tenant query filters and audit fields keep working.</summary>
public static class AmbientUser
{
    private static readonly AsyncLocal<(Guid? TenantId, Guid? UserId)?> Current = new();

    public static (Guid? TenantId, Guid? UserId)? Value => Current.Value;

    public static IDisposable Use(Guid? tenantId, Guid? userId)
    {
        var previous = Current.Value;
        Current.Value = (tenantId, userId);
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly (Guid? TenantId, Guid? UserId)? _previous;
        public Scope((Guid? TenantId, Guid? UserId)? previous) => _previous = previous;
        public void Dispose() => Current.Value = _previous;
    }
}
