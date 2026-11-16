using System.Text.Json;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>Who is asking and what the user is looking at; tools never trust anything else.</summary>
public sealed class AssistantToolContext
{
    public JsonElement UiContext { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TenantId { get; init; }
    public string? Role { get; init; }
}

public sealed record AssistantParameter(string Name, string Type, string Description, bool Required = false, string[]? Enum = null);

/// <summary>A read-only tool, or (via <see cref="IAssistantWriteTool"/>) a change that waits for user confirmation.</summary>
public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<AssistantParameter> Parameters { get; }

    /// <summary>Read tools return their data. Write tools validate and return an <see cref="AssistantProposal"/>; nothing is changed yet.</summary>
    Task<object> ExecuteAsync(AssistantToolContext context, JsonElement arguments, CancellationToken cancellationToken);
}

public interface IAssistantWriteTool : IAssistantTool
{
    /// <summary>Applies the change after the user confirmed it. Returns a short result message.</summary>
    Task<string> ApplyAsync(AssistantToolContext context, JsonElement arguments, CancellationToken cancellationToken);
}

/// <summary>ArgumentsJson replaces the model's raw arguments when the change is applied (use it to pin resolved ids).</summary>
public sealed record AssistantProposal(string Label, string Summary, string? ArgumentsJson = null);

public sealed record AssistantPendingAction(Guid ActionId, string Label, string Summary);

public sealed class AssistantToolOutcome
{
    public required object Result { get; init; }
    public AssistantPendingAction? Action { get; init; }
}

public sealed record AssistantConfirmResult(bool Success, string Message);

public interface IAssistantToolRegistry
{
    /// <summary>Tool definitions in the OpenAI-compatible function-calling format.</summary>
    IReadOnlyList<object> Definitions();
    bool Has(string name);
    Task<AssistantToolOutcome> ExecuteAsync(string name, JsonElement arguments, AssistantToolContext context, CancellationToken cancellationToken);
    Task<AssistantConfirmResult> ConfirmAsync(Guid actionId, AssistantToolContext context, CancellationToken cancellationToken);
    AssistantConfirmResult Cancel(Guid actionId, AssistantToolContext context);
}
