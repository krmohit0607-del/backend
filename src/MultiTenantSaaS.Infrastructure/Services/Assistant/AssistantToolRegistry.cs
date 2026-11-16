using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

/// <summary>Finds and runs assistant tools and holds proposed changes until the user confirms them.</summary>
public sealed class AssistantToolRegistry : IAssistantToolRegistry
{
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(15);
    private readonly Dictionary<string, IAssistantTool> _tools;
    private readonly IMemoryCache _cache;
    private readonly IAuditService _audit;
    private readonly ILogger<AssistantToolRegistry> _logger;

    public AssistantToolRegistry(IEnumerable<IAssistantTool> tools, IMemoryCache cache, IAuditService audit, ILogger<AssistantToolRegistry> logger)
    {
        _tools = tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);
        _cache = cache;
        _audit = audit;
        _logger = logger;
    }

    public bool Has(string name) => _tools.ContainsKey(name);

    public IReadOnlyList<object> Definitions() => _tools.Values.Select(tool => (object)new
    {
        type = "function",
        function = new
        {
            name = tool.Name,
            description = tool.Description,
            parameters = new
            {
                type = "object",
                properties = tool.Parameters.ToDictionary(
                    p => p.Name,
                    p => p.Enum is null
                        ? (object)new { type = p.Type, description = p.Description }
                        : new { type = p.Type, description = p.Description, @enum = p.Enum }),
                required = tool.Parameters.Where(p => p.Required).Select(p => p.Name).ToArray(),
                additionalProperties = false
            }
        }
    }).ToList();

    public async Task<AssistantToolOutcome> ExecuteAsync(string name, JsonElement arguments, AssistantToolContext context, CancellationToken cancellationToken)
    {
        if (!_tools.TryGetValue(name, out var tool))
            return new AssistantToolOutcome { Result = new { error = "Unknown tool." } };

        object result;
        try
        {
            result = await tool.ExecuteAsync(context, arguments, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Assistant tool {Tool} failed.", name);
            return new AssistantToolOutcome { Result = new { error = "The tool failed to read the data. Tell the user it could not be retrieved." } };
        }

        if (tool is not IAssistantWriteTool || result is not AssistantProposal proposal)
            return new AssistantToolOutcome { Result = result };

        var actionId = Guid.NewGuid();
        _cache.Set(CacheKey(actionId), new PendingEntry(context.UserId, context.TenantId, name, proposal.ArgumentsJson ?? arguments.GetRawText(), proposal.Label), PendingLifetime);
        return new AssistantToolOutcome
        {
            Result = new
            {
                proposed = true,
                summary = proposal.Summary,
                requiresUserConfirmation = true,
                note = "Nothing has been changed yet. Tell the user to review the proposal and press the confirm button."
            },
            Action = new AssistantPendingAction(actionId, proposal.Label, proposal.Summary)
        };
    }

    public async Task<AssistantConfirmResult> ConfirmAsync(Guid actionId, AssistantToolContext context, CancellationToken cancellationToken)
    {
        if (!TryTake(actionId, context, out var entry))
            return new AssistantConfirmResult(false, "This proposal has expired or was already handled. Ask the agent to propose it again.");
        if (_tools.GetValueOrDefault(entry.Tool) is not IAssistantWriteTool tool)
            return new AssistantConfirmResult(false, "This action is no longer available.");

        try
        {
            using var document = JsonDocument.Parse(entry.ArgumentsJson);
            var message = await tool.ApplyAsync(context, document.RootElement, cancellationToken);
            await _audit.LogAsync("AssistantAction", entry.Tool, null, null, entry.ArgumentsJson, context.TenantId, context.UserId, cancellationToken);
            return new AssistantConfirmResult(true, message);
        }
        catch (AssistantToolException ex)
        {
            return new AssistantConfirmResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Assistant action {Tool} failed to apply.", entry.Tool);
            return new AssistantConfirmResult(false, "The change could not be applied. Nothing was saved.");
        }
    }

    public AssistantConfirmResult Cancel(Guid actionId, AssistantToolContext context) =>
        TryTake(actionId, context, out _)
            ? new AssistantConfirmResult(true, "Proposal discarded.")
            : new AssistantConfirmResult(false, "This proposal has expired or was already handled.");

    private bool TryTake(Guid actionId, AssistantToolContext context, out PendingEntry entry)
    {
        entry = default!;
        if (!_cache.TryGetValue(CacheKey(actionId), out PendingEntry? found) || found is null) return false;
        if (found.UserId != context.UserId || found.TenantId != context.TenantId) return false;
        _cache.Remove(CacheKey(actionId));
        entry = found;
        return true;
    }

    private static string CacheKey(Guid actionId) => $"assistant-action:{actionId}";

    private sealed record PendingEntry(Guid? UserId, Guid? TenantId, string Tool, string ArgumentsJson, string Label);
}

/// <summary>A user-facing failure while applying a confirmed change.</summary>
public sealed class AssistantToolException : Exception
{
    public AssistantToolException(string message) : base(message) { }
}
