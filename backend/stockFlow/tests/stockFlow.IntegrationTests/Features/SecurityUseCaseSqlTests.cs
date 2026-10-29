using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Features;
using stockFlow.Application.Common.Errors;
using stockFlow.Application.Common.Pagination;
using Xunit;
namespace stockFlow.IntegrationTests.Features;
[Trait("Category", "Sql")]
public sealed class SecurityUseCaseSqlTests
{
    [Fact] public async Task Refresh_rotation_detects_reuse_and_revokes_the_family()
    {
        await using var f = new BackendFixture(); await f.Initialize();
        var login = await f.Call<IAuthService, TokenResult>(null, s => s.LoginAsync(new("admin@example.test", BackendFixture.Password), default));
        var next = await f.Call<IAuthService, TokenResult>(null, s => s.RefreshAsync(login.RefreshToken, default));
        Assert.Equal(DateTimeKind.Utc, next.RefreshExpiresAt.Kind);
        Assert.Null(await f.Call<IAuthService, PrincipalInfo?>(null, s => s.ValidateAsync(login.AccessToken, default)));
        Assert.NotNull(await f.Call<IAuthService, PrincipalInfo?>(null, s => s.ValidateAsync(next.AccessToken, default)));
        await Assert.ThrowsAsync<AppException>(() => f.Call<IAuthService, TokenResult>(null, s => s.RefreshAsync(login.RefreshToken, default)));
        Assert.Null(await f.Call<IAuthService, PrincipalInfo?>(null, s => s.ValidateAsync(next.AccessToken, default)));
        await using var db = f.Database.CreateContext();
        Assert.DoesNotContain(await db.AuthSessions.Select(x => x.AccessHash).ToListAsync(), x => x == login.AccessToken || x == next.AccessToken);
    }
    [Fact] public async Task Assignments_filter_lists_and_hide_details_and_transfer_needs_both_warehouses()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, b, p, _) = await f.Catalog();
        var user = await f.Admin<IUserService, UserView>(s => s.CreateAsync(new("keeper@example.test", BackendFixture.Password, Roles.WarehouseKeeper, [a]), default));
        var doc = await f.Create("openings", new(b, null, null, null, null, [new(p, 1)]));
        var list = await f.Call<IDocumentService, PagedResult<DocumentView>>(user.Id, s => s.ListAsync("openings", new(), null, null, default)); Assert.Empty(list.Items);
        var missing = await Assert.ThrowsAsync<AppException>(() => f.Call<IDocumentService, DocumentView>(user.Id, s => s.GetAsync("openings", doc.Id, default))); Assert.Equal(ErrorKind.NotFound, missing.Kind);
        var forbidden = await Assert.ThrowsAsync<AppException>(() => f.Call<IDocumentService, DocumentView>(user.Id, s => s.CreateAsync("transfers", new(a, null, b, null, null, [new(p, 1)]), default))); Assert.Equal(ErrorKind.Forbidden, forbidden.Kind);
        await Assert.ThrowsAsync<AppException>(() => f.Call<IMasterService, MasterView>(user.Id, s => s.CreateAsync("categories", new("Denied"), default)));
    }
    [Fact] public async Task Last_admin_is_protected_and_permissions_change_revokes_sessions()
    {
        await using var f = new BackendFixture(); await f.Initialize();
        var admin = await f.Admin<IAuthService, UserView>(s => s.MeAsync(default));
        Assert.Equal("last_admin", (await Assert.ThrowsAsync<AppException>(() => f.Admin<IUserService, UserView>(s => s.UpdateAsync(admin.Id, new(Roles.Viewer, false, []), Convert.FromBase64String(admin.Version), default)))).Code);
        var user = await f.Admin<IUserService, UserView>(s => s.CreateAsync(new("viewer@example.test", BackendFixture.Password, Roles.Viewer, []), default));
        var login = await f.Call<IAuthService, TokenResult>(null, s => s.LoginAsync(new("viewer@example.test", BackendFixture.Password), default));
        await f.Admin<IUserService, UserView>(s => s.UpdateAsync(user.Id, new(Roles.Viewer, false, []), Convert.FromBase64String(user.Version), default));
        Assert.Null(await f.Call<IAuthService, PrincipalInfo?>(null, s => s.ValidateAsync(login.AccessToken, default)));
    }
    [Fact] public async Task Cancelled_receipt_reopens_without_creating_a_second_receipt()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, _, p, supplier) = await f.Catalog();
        var order = await f.Create("purchase-orders", new(a, supplier, null, null, null, [new(p, 4, 2)])); await f.Act(order, "submit");
        var receipt = await f.Admin<IDocumentService, DocumentView>(s => s.ReceiveAsync(new(order.Id, null), default));
        var cancelled = await f.Act(receipt, "cancel"); var reopened = await f.Act(cancelled, "reopen"); await f.Act(reopened, "post");
        await using var db = f.Database.CreateContext(); Assert.Single(await db.StockReceipts.ToListAsync()); Assert.Equal(4, await db.StockBalances.Select(x => x.Quantity).SingleAsync());
    }
    [Fact] public async Task Used_product_unit_cannot_change_and_inactive_reference_cannot_post()
    {
        await using var f = new BackendFixture(); await f.Initialize(); var (a, _, p, _) = await f.Catalog();
        var op = await f.Create("openings", new(a, null, null, null, null, [new(p, 3)]));
        var product = await f.Admin<IMasterService, MasterView>(s => s.GetAsync("products", p, default));
        var unit = await f.Master("units", new("Kilogram", "KG"));
        Assert.Equal("product_used", (await Assert.ThrowsAsync<AppException>(() => f.Admin<IMasterService, MasterView>(s => s.UpdateAsync("products", p,
            new(product.Name, product.Code, CategoryId: product.CategoryId, UnitId: unit.Id), Convert.FromBase64String(product.Version), default)))).Code);
        await f.Admin<IMasterService, MasterView>(s => s.SetActiveAsync("products", p, false, Convert.FromBase64String(product.Version), default));
        await Assert.ThrowsAsync<AppException>(() => f.Act(op, "post"));
        await using var db = f.Database.CreateContext(); Assert.Empty(await db.StockMovements.ToListAsync());
    }
}
