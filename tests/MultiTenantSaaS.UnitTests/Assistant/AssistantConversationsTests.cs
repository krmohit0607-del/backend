using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.API.Controllers;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Domain.Entities.Assistant;
using MultiTenantSaaS.Infrastructure.Persistence;

namespace MultiTenantSaaS.UnitTests.Assistant;

public class AssistantConversationsTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private static (AssistantConversationsController Controller, ApplicationDbContext Db) Setup()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Tenant);
        user.Setup(u => u.UserId).Returns(Me);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        return (new AssistantConversationsController(db, user.Object), db);
    }

    private static AssistantConversation Chat(Guid owner, string title, params string[] messages)
    {
        var conversation = new AssistantConversation { TenantId = Tenant, UserId = owner, Title = title, LastMessageAtUtc = DateTime.UtcNow };
        var at = DateTime.UtcNow;
        foreach (var text in messages)
            conversation.Messages.Add(new AssistantMessage { TenantId = Tenant, Role = "user", Content = text, CreatedAtUtc = at = at.AddSeconds(1) });
        return conversation;
    }

    private static T Data<T>(IActionResult result) =>
        ((ApiResponse<T>)((OkObjectResult)result).Value!).Data!;

    [Fact]
    public async Task ListShowsOnlyMyChatsNewestFirstAndSearchesTitlesAndMessages()
    {
        var (controller, db) = Setup();
        var older = Chat(Me, "Fuel question", "How much fuel for Tema?");
        older.LastMessageAtUtc = DateTime.UtcNow.AddHours(-2);
        db.AssistantConversations.AddRange(older, Chat(Me, "Overdue payables", "list them"), Chat(Other, "Not mine", "secret"));
        await db.SaveChangesAsync();

        var all = Data<List<ConversationSummaryDto>>(await controller.List(null, default));
        var byMessage = Data<List<ConversationSummaryDto>>(await controller.List("tema", default));

        all.Select(c => c.Title).Should().Equal("Overdue payables", "Fuel question");
        byMessage.Should().ContainSingle().Which.Title.Should().Be("Fuel question");
    }

    [Fact]
    public async Task GetReturnsMessagesInOrderAndHidesOtherUsersChats()
    {
        var (controller, db) = Setup();
        var mine = Chat(Me, "Mine", "first", "second");
        var theirs = Chat(Other, "Theirs", "x");
        db.AssistantConversations.AddRange(mine, theirs);
        await db.SaveChangesAsync();

        var detail = Data<ConversationDetailDto>(await controller.Get(mine.Id, default));

        detail.Messages.Select(m => m.Content).Should().Equal("first", "second");
        (await controller.Get(theirs.Id, default)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RenameAndDeleteOnlyWorkOnMyChats()
    {
        var (controller, db) = Setup();
        var mine = Chat(Me, "Mine", "a");
        var theirs = Chat(Other, "Theirs", "b");
        db.AssistantConversations.AddRange(mine, theirs);
        await db.SaveChangesAsync();

        await controller.Rename(mine.Id, new RenameConversationRequest { Title = "  Renamed  " }, default);
        (await controller.Rename(theirs.Id, new RenameConversationRequest { Title = "Hacked" }, default)).Should().BeOfType<NotFoundObjectResult>();
        (await controller.Rename(mine.Id, new RenameConversationRequest { Title = " " }, default)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Delete(theirs.Id, default)).Should().BeOfType<NotFoundObjectResult>();
        await controller.Delete(mine.Id, default);

        (await db.AssistantConversations.AsNoTracking().SingleAsync()).Title.Should().Be("Theirs");
        (await db.AssistantMessages.AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAllRemovesOnlyMyChatsAndMessages()
    {
        var (controller, db) = Setup();
        db.AssistantConversations.AddRange(Chat(Me, "One", "a"), Chat(Me, "Two", "b"), Chat(Other, "Theirs", "c"));
        await db.SaveChangesAsync();

        await controller.DeleteAll(default);

        (await db.AssistantConversations.AsNoTracking().SingleAsync()).Title.Should().Be("Theirs");
        (await db.AssistantMessages.AsNoTracking().SingleAsync()).Content.Should().Be("c");
    }
}
