using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Features;
using stockFlow.Infrastructure.Persistence;
using stockFlow.IntegrationTests.Features;
using Xunit;
namespace stockFlow.IntegrationTests.Api;
[Trait("Category", "Sql")]
public sealed class BusinessApiTests
{
    sealed class Factory(string connection) : WebApplicationFactory<stockFlow.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production")
            .UseSetting("ConnectionStrings:DefaultConnection", connection);
    }
    [Fact] public async Task Http_auth_permissions_validation_etags_and_stock_post_work_together()
    {
        await using var f = new BackendFixture(); await f.Initialize();
        await using var factory = new Factory(f.Database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products")).StatusCode);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@example.test", BackendFixture.Password)); loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<TokenResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/categories", new MasterInput("Food")); response.EnsureSuccessStatusCode();
        var category = (await response.Content.ReadFromJsonAsync<MasterView>())!;
        Assert.Equal("\"" + category.Version + "\"", response.Headers.ETag!.ToString());
        var missingMatch = await client.PutAsJsonAsync($"/api/v1/categories/{category.Id}", new MasterInput("Other")); Assert.Equal(428, (int)missingMatch.StatusCode);
        using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/categories/{category.Id}") { Content = JsonContent.Create(new MasterInput("Other")) };
        put.Headers.TryAddWithoutValidation("If-Match", response.Headers.ETag.ToString());
        var updated = await client.SendAsync(put); updated.EnsureSuccessStatusCode();
        using var stale = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/categories/{category.Id}") { Content = JsonContent.Create(new MasterInput("Stale")) };
        stale.Headers.TryAddWithoutValidation("If-Match", response.Headers.ETag.ToString()); Assert.Equal(412, (int)(await client.SendAsync(stale)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/products", new { name = "Missing references" })).StatusCode);
        var (a, _, p, _) = await f.Catalog();
        var created = await client.PostAsJsonAsync("/api/v1/openings", new DocumentInput(a, null, null, null, null, [new(p, 10)])); created.EnsureSuccessStatusCode();
        var document = (await created.Content.ReadFromJsonAsync<DocumentView>())!;
        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/openings/{document.Id}/post"); post.Headers.TryAddWithoutValidation("If-Match", created.Headers.ETag!.ToString());
        (await client.SendAsync(post)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products")).StatusCode);
    }
}
