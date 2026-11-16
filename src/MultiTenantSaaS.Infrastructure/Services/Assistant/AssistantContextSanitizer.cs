using System.Text.Json;
using System.Text.Json.Nodes;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

/// <summary>Removes personal, credential and bank-detail properties before data reaches the LLM.</summary>
public static class AssistantContextSanitizer
{
    private static readonly string[] BlockedFragments =
        ["email", "phone", "mobile", "password", "token", "secret", "apikey", "history", "iban", "swift", "accountnumber"];

    public static string Sanitize(JsonElement context)
    {
        if (context.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return "{}";
        var node = JsonNode.Parse(context.GetRawText());
        Strip(node);
        return node?.ToJsonString() ?? "{}";
    }

    public static JsonNode? SanitizeNode(JsonNode? node)
    {
        Strip(node);
        return node;
    }

    private static void Strip(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(pair => pair.Key).Where(IsBlocked).ToList()) obj.Remove(key);
                foreach (var pair in obj) Strip(pair.Value);
                break;
            case JsonArray array:
                foreach (var item in array) Strip(item);
                break;
        }
    }

    private static bool IsBlocked(string key) =>
        BlockedFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
