using System.Text.Json;
using Xunit;
namespace stockFlow.IntegrationTests.Api;
[Trait("Category", "Api")]
public sealed class OpenApiContractTests
{
    [Fact] public async Task Contract_contains_business_routes_permissions_and_concurrency_headers()
    {
        await using var factory = new ApiTestFactory("Development"); using var client = factory.NewClient();
        var response = await client.GetAsync("/openapi/v1.json"); response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(); using var doc = JsonDocument.Parse(text);
        var paths = doc.RootElement.GetProperty("paths");
        foreach (var route in new[] { "products", "warehouses", "users", "purchase-orders", "receipts", "openings", "issues", "transfers" })
            Assert.True(paths.TryGetProperty("/api/v1/" + route, out _));
        Assert.True(paths.GetProperty("/api/v1/products").GetProperty("get").TryGetProperty("security", out _));
        var post = paths.GetProperty("/api/v1/issues/{id}/post").GetProperty("post");
        Assert.Contains(post.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == "If-Match" && x.GetProperty("required").GetBoolean());
        Assert.True(post.GetProperty("responses").TryGetProperty("412", out _));
        Assert.Equal("Opaque session token", doc.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("bearerFormat").GetString());
        var export = Environment.GetEnvironmentVariable("STOCKFLOW_EXPORT_OPENAPI");
        if (!string.IsNullOrWhiteSpace(export)) await File.WriteAllTextAsync(export, text);
    }
    [Fact] public async Task Authentication_rate_limit_returns_safe_429_and_retry_after_without_SQL()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        for (var i = 0; i < 20; i++)
        {
            using var invalid = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/auth/login", invalid)).StatusCode);
        }
        using var payload = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var limited = await client.PostAsync("/api/v1/auth/login", payload);
        Assert.Equal(429, (int)limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
        using var json = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        Assert.Equal("too_many_requests", json.RootElement.GetProperty("code").GetString());
    }
}
