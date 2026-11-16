using System.Text.Json;
using FluentAssertions;
using MultiTenantSaaS.Infrastructure.Services.Assistant;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Assistant;

public class AssistantContextSanitizerTests
{
    [Fact]
    public void Removes_personal_and_credential_keys_at_any_depth()
    {
        using var doc = JsonDocument.Parse("""
            {"activeVoyage":{"id":"v1","ownerEmail":"a@b.c","notes":"ok"},
             "vessels":[{"name":"MV A","contactPhone":"123","apiToken":"x"}],
             "chatHistory":[1,2],"password":"p"}
            """);

        var json = AssistantContextSanitizer.Sanitize(doc.RootElement);

        json.Should().Contain("\"id\":\"v1\"").And.Contain("MV A").And.Contain("\"notes\":\"ok\"");
        json.Should().NotContainEquivalentOf("email").And.NotContainEquivalentOf("phone")
            .And.NotContainEquivalentOf("token").And.NotContainEquivalentOf("history").And.NotContainEquivalentOf("password");
    }

    [Fact]
    public void Missing_context_becomes_empty_object()
    {
        AssistantContextSanitizer.Sanitize(default).Should().Be("{}");
    }
}
