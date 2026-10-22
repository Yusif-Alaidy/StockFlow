using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace stockFlow.IntegrationTests.Api;

[Trait("Category", "Api")]
public class ApiConventionsTests
{
    private static async Task<JsonElement> ReadProblem(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        Assert.Equal(status, json.GetProperty("status").GetInt32());
        Assert.Equal(code, json.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        return json;
    }

    [Fact]
    public async Task Liveness_is_available_without_SQL_and_readiness_can_be_healthy()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        foreach (var route in new[] { "/health/live", "/health/ready" })
        {
            using var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
            Assert.True(response.Headers.CacheControl!.NoStore);
        }
    }

    [Fact]
    public async Task Readiness_failure_returns_safe_503_but_liveness_remains_healthy()
    {
        await using var factory = new ApiTestFactory(status: HealthStatus.Unhealthy); using var client = factory.NewClient();
        using var response = await client.GetAsync("/health/ready");
        await ReadProblem(response, 503, "service_unavailable");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Model_validation_and_malformed_JSON_use_problem_details()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        foreach (var body in new[] { "{}", "{broken" })
        {
            using var response = await client.PostAsync("/__tests/validation", new StringContent(body, Encoding.UTF8, "application/json"));
            var json = await ReadProblem(response, 400, "validation_failed");
            Assert.True(json.GetProperty("errors").EnumerateObject().Any());
        }
    }

    [Theory]
    [InlineData("/route-that-does-not-exist", 404, "not_found")]
    [InlineData("/__tests/missing", 404, "not_found")]
    [InlineData("/__tests/forbidden", 403, "forbidden")]
    [InlineData("/__tests/unauthorized", 401, "unauthorized")]
    [InlineData("/__tests/conflict", 409, "document_conflict")]
    public async Task Expected_errors_have_consistent_shape(string route, int status, string code)
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var response = await client.GetAsync(route); await ReadProblem(response, status, code);
    }

    [Fact]
    public async Task Unexpected_exception_does_not_leak_details_in_body_or_application_logs()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__tests/error?password=SECRET_QUERY");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer SECRET_TOKEN");
        using var response = await client.SendAsync(request);
        await ReadProblem(response, 500, "server_error");
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SECRET_CONNECTION_PASSWORD", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("stackTrace", body);
        Assert.DoesNotContain(factory.Messages, message => message.Contains("SECRET_CONNECTION_PASSWORD", StringComparison.Ordinal));
        Assert.DoesNotContain(factory.Messages, message => message.Contains("SECRET_QUERY", StringComparison.Ordinal) || message.Contains("SECRET_TOKEN", StringComparison.Ordinal));
        Assert.Contains(factory.Messages, message => message.Contains("Unhandled exception", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cors_allows_configured_origin_and_exposes_ETag()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "http://localhost:4200");
        using var response = await client.SendAsync(request);
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("etag", string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers")).ToLowerInvariant());
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        preflight.Headers.Add("Origin", "http://localhost:4200");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,if-match");
        using var allowed = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.True(allowed.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_does_not_grant_unlisted_origin()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://unlisted.example");
        using var response = await client.SendAsync(request);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task OpenApi_and_UI_are_hidden_in_production_by_default()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var schema = await client.GetAsync("/openapi/v1.json");
        await ReadProblem(schema, 404, "not_found");
        using var ui = await client.GetAsync("/docs/index.html");
        await ReadProblem(ui, 404, "not_found");
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    public async Task Enabled_documentation_exposes_real_OpenApi_and_local_UI(string environment, bool expose)
    {
        await using var factory = new ApiTestFactory(environment, exposeDocs: expose); using var client = factory.NewClient();
        using var schema = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, schema.StatusCode);
        var json = JsonDocument.Parse(await schema.Content.ReadAsStringAsync());
        Assert.Equal("StockFlow API", json.RootElement.GetProperty("info").GetProperty("title").GetString());
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health/live", out _));
        Assert.True(paths.TryGetProperty("/health/ready", out _));
        Assert.DoesNotContain(paths.EnumerateObject(), path => path.Name.StartsWith("/__tests", StringComparison.Ordinal));
        Assert.True(paths.GetProperty("/health/ready").GetProperty("get").GetProperty("responses").TryGetProperty("503", out _));
        using var ui = await client.GetAsync("/docs/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        Assert.Contains("swagger-ui", await ui.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ETag_utility_returns_428_400_412_and_accepts_current_version()
    {
        await using var factory = new ApiTestFactory(); using var client = factory.NewClient();
        using var missing = await client.GetAsync("/__tests/etag"); await ReadProblem(missing, 428, "precondition_required");
        foreach (var value in new[] { "*", "W/\"AAAAAAAAAAE=\"", "\"bad\"", "\"AAAAAAAAAAE=\", \"AAAAAAAAAAE=\"" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/__tests/etag");
            request.Headers.TryAddWithoutValidation("If-Match", value);
            using var response = await client.SendAsync(request); await ReadProblem(response, 400, "invalid_etag");
        }
        using var stale = new HttpRequestMessage(HttpMethod.Get, "/__tests/etag");
        stale.Headers.TryAddWithoutValidation("If-Match", "\"AAAAAAAAAAI=\"");
        using var staleResponse = await client.SendAsync(stale); await ReadProblem(staleResponse, 412, "precondition_failed");
        using var current = new HttpRequestMessage(HttpMethod.Get, "/__tests/etag");
        current.Headers.TryAddWithoutValidation("If-Match", "\"AAAAAAAAAAE=\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(current)).StatusCode);
    }
}
