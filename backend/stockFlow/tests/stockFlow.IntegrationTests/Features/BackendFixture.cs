using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Abstractions;
using stockFlow.Application.Features;
using stockFlow.Infrastructure;
using stockFlow.Infrastructure.Persistence;
using stockFlow.IntegrationTests.Persistence;
namespace stockFlow.IntegrationTests.Features;
internal sealed class BackendFixture : IAsyncDisposable
{
    public SqlTestDatabase Database { get; } = new();
    public Guid AdminId { get; private set; }
    public const string Password = "Test-only-Strong-12345";
    public async Task Initialize()
    {
        await Database.InitializeAsync();
        await Call<IAuthService>(null, a => a.BootstrapAsync("admin@example.test", Password, default));
        await using var db = Database.CreateContext(); AdminId = await db.Users.Select(x => x.Id).SingleAsync();
    }
    ServiceProvider Provider(Guid? actor) => new ServiceCollection().AddSingleton<ICurrentUser>(new Actor(actor)).AddInfrastructure(Database.ConnectionString).BuildServiceProvider();
    public async Task<T> Call<S, T>(Guid? actor, Func<S, Task<T>> run) where S : notnull
    { await using var provider = Provider(actor); await using var scope = provider.CreateAsyncScope(); return await run(scope.ServiceProvider.GetRequiredService<S>()); }
    public async Task Call<S>(Guid? actor, Func<S, Task> run) where S : notnull
    { await using var provider = Provider(actor); await using var scope = provider.CreateAsyncScope(); await run(scope.ServiceProvider.GetRequiredService<S>()); }
    public Task<T> Admin<S, T>(Func<S, Task<T>> run) where S : notnull => Call(AdminId, run);
    public Task<MasterView> Master(string resource, MasterInput input) => Admin<IMasterService, MasterView>(s => s.CreateAsync(resource, input, default));
    public async Task<(Guid Warehouse, Guid Destination, Guid Product, Guid Supplier)> Catalog()
    {
        var b = await Master("branches", new("Main", "MAIN"));
        var a = await Master("warehouses", new("A", "A", BranchId: b.Id));
        var dest = await Master("warehouses", new("B", "B", BranchId: b.Id));
        var c = await Master("categories", new("Food")); var u = await Master("units", new("Piece", "PCS"));
        var p = await Master("products", new("Rice", "RICE", CategoryId: c.Id, UnitId: u.Id));
        var supplier = await Master("suppliers", new("Supplier", "SUP"));
        return (a.Id, dest.Id, p.Id, supplier.Id);
    }
    public Task<DocumentView> Create(string kind, DocumentInput request) => Admin<IDocumentService, DocumentView>(s => s.CreateAsync(kind, request, default));
    public Task<DocumentView> Act(DocumentView doc, string action, string? version = null) => Admin<IDocumentService, DocumentView>(s => s.ActAsync(doc.Kind, doc.Id, action, Convert.FromBase64String(version ?? doc.Version), default));
    public ValueTask DisposeAsync() => Database.DisposeAsync();
    sealed record Actor(Guid? UserId) : ICurrentUser { public bool IsAuthenticated => UserId.HasValue; }
}
