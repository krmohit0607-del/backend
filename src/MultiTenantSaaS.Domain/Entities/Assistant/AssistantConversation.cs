using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Assistant;

/// <summary>One AI-agent chat thread. Private to the user who started it.</summary>
public class AssistantConversation : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ICollection<AssistantMessage> Messages { get; set; } = new List<AssistantMessage>();
}

public class AssistantMessage : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>user | assistant</summary>
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;

    /// <summary>Navigation actions only; confirmations are short-lived and not kept.</summary>
    public string? ActionsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual AssistantConversation Conversation { get; set; } = null!;
}
