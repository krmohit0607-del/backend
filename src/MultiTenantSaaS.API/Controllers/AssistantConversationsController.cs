using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>The signed-in user's saved AI-agent chats. A user can only see and change their own.</summary>
[ApiController]
[Route("api/assistant/conversations")]
[Authorize]
public sealed class AssistantConversationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssistantConversationsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var query = _db.AssistantConversations.AsNoTracking().Where(c => c.UserId == userId);
        var q = search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(q))
            query = query.Where(c => c.Title.ToLower().Contains(q) || c.Messages.Any(m => m.Content.ToLower().Contains(q)));

        var rows = await query.OrderByDescending(c => c.LastMessageAtUtc).Take(200)
            .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.LastMessageAtUtc, c.Messages.Count))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ConversationSummaryDto>>.SuccessResult(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var conversation = await _db.AssistantConversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
        if (conversation is null) return NotFound(ApiResponse<ConversationDetailDto>.FailureResult("Conversation not found."));

        var messages = await _db.AssistantMessages.AsNoTracking().Where(m => m.ConversationId == id).OrderBy(m => m.CreatedAtUtc).ToListAsync(cancellationToken);
        var dto = new ConversationDetailDto(conversation.Id, conversation.Title, conversation.LastMessageAtUtc,
            messages.Select(m => new ConversationMessageDto(m.Id, m.Role, m.Content, m.CreatedAtUtc, ReadActions(m.ActionsJson))).ToList());
        return Ok(ApiResponse<ConversationDetailDto>.SuccessResult(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RenameConversationRequest? request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var conversation = new Domain.Entities.Assistant.AssistantConversation
        {
            TenantId = _currentUser.TenantId,
            UserId = userId,
            Title = CleanTitle(request?.Title) ?? "New chat"
        };
        _db.AssistantConversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<ConversationSummaryDto>.SuccessResult(new ConversationSummaryDto(conversation.Id, conversation.Title, conversation.LastMessageAtUtc, 0)));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameConversationRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var title = CleanTitle(request.Title);
        if (title is null) return BadRequest(ApiResponse<ConversationSummaryDto>.FailureResult("Enter a title of up to 200 characters."));
        var conversation = await _db.AssistantConversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
        if (conversation is null) return NotFound(ApiResponse<ConversationSummaryDto>.FailureResult("Conversation not found."));

        conversation.Title = title;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<ConversationSummaryDto>.SuccessResult(new ConversationSummaryDto(conversation.Id, conversation.Title, conversation.LastMessageAtUtc, 0)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var conversation = await _db.AssistantConversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
        if (conversation is null) return NotFound(ApiResponse.FailureResult("Conversation not found."));

        _db.AssistantMessages.RemoveRange(await _db.AssistantMessages.Where(m => m.ConversationId == id).ToListAsync(cancellationToken));
        _db.AssistantConversations.Remove(conversation);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.SuccessResult("Conversation deleted."));
    }

    /// <summary>Deletes all of the signed-in user's chats.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var conversations = await _db.AssistantConversations.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        var ids = conversations.Select(c => c.Id).ToList();
        _db.AssistantMessages.RemoveRange(await _db.AssistantMessages.Where(m => ids.Contains(m.ConversationId)).ToListAsync(cancellationToken));
        _db.AssistantConversations.RemoveRange(conversations);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.SuccessResult("All conversations deleted."));
    }

    private static string? CleanTitle(string? title)
    {
        var trimmed = title?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > 200 ? null : trimmed;
    }

    private static List<AssistantAction> ReadActions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<AssistantAction>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []; }
        catch (JsonException) { return []; }
    }
}

public sealed record ConversationSummaryDto(Guid Id, string Title, DateTime LastMessageAtUtc, int MessageCount);

public sealed record ConversationMessageDto(Guid Id, string Role, string Content, DateTime CreatedAtUtc, List<AssistantAction> Actions);

public sealed record ConversationDetailDto(Guid Id, string Title, DateTime LastMessageAtUtc, List<ConversationMessageDto> Messages);

public sealed class RenameConversationRequest
{
    public string? Title { get; set; }
}
