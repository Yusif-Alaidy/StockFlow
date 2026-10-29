using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;
using stockFlow.Infrastructure.Persistence;
namespace stockFlow.Infrastructure.Features;
internal sealed class MasterService(StockFlowDbContext db, Access access) : IMasterService
{
    public Task<PagedResult<MasterView>> ListAsync(string resource, PageRequest page, string? search, bool? active, CancellationToken ct) => resource switch
    {
        "branches" => List(db.Branches, page, search, active, resource, ct),
        "warehouses" => List(db.Warehouses, page, search, active, resource, ct),
        "categories" => List(db.Categories, page, search, active, resource, ct),
        "units" => List(db.Units, page, search, active, resource, ct),
        "products" => List(db.Products, page, search, active, resource, ct),
        "suppliers" => List(db.Suppliers, page, search, active, resource, ct),
        _ => throw Access.Missing()
    };
    async Task<PagedResult<MasterView>> List<T>(IQueryable<T> query, PageRequest page, string? search, bool? active, string resource, CancellationToken ct) where T : BaseEntity
    {
        var skip = page.GetSkip(); var allowed = await access.Warehouses(ct);
        if (allowed is not null && resource == "warehouses") query = query.Where(x => allowed.Contains(x.Id));
        if (allowed is not null && resource == "branches") query = query.Where(x => db.Warehouses.Any(w => w.BranchId == x.Id && allowed.Contains(w.Id)));
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100) throw Access.Invalid("Search maximum length is 100.");
            var value = search.Trim(); query = query.Where(x => EF.Property<string>(x, "Name").Contains(value));
        }
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderBy(x => EF.Property<string>(x, "Name")).ThenBy(x => x.Id).Skip(skip).Take(page.PageSize).ToListAsync(ct);
        return new(rows.Select(View).ToList(), page.PageNumber, page.PageSize, total);
    }
    async Task<BaseEntity> Load(string resource, Guid id, CancellationToken ct)
    {
        BaseEntity? entity = resource switch
        {
            "branches" => await db.Branches.FindAsync([id], ct), "warehouses" => await db.Warehouses.FindAsync([id], ct),
            "categories" => await db.Categories.FindAsync([id], ct), "units" => await db.Units.FindAsync([id], ct),
            "products" => await db.Products.FindAsync([id], ct), "suppliers" => await db.Suppliers.FindAsync([id], ct), _ => null
        };
        if (entity is null) throw Access.Missing();
        var allowed = await access.Warehouses(ct);
        if (allowed is not null && entity is Warehouse && !allowed.Contains(id)) throw Access.Missing();
        if (allowed is not null && entity is Branch && !await db.Warehouses.AnyAsync(w => w.BranchId == id && allowed.Contains(w.Id), ct)) throw Access.Missing();
        return entity;
    }
    MasterView View(BaseEntity entity)
    {
        var version = Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        return entity switch
        {
            Branch x => new(x.Id, x.Name, x.Code, x.IsActive, null, null, null, null, null, null, version),
            Warehouse x => new(x.Id, x.Name, x.Code, x.IsActive, x.BranchId, null, null, null, null, null, version),
            Category x => new(x.Id, x.Name, null, x.IsActive, null, null, null, null, null, null, version),
            Unit x => new(x.Id, x.Name, x.Code, x.IsActive, null, null, null, null, null, null, version),
            Product x => new(x.Id, x.Name, x.Sku, x.IsActive, null, x.CategoryId, x.UnitId, null, null, null, version),
            Supplier x => new(x.Id, x.Name, x.Code, x.IsActive, null, null, null, x.Email, x.Phone, x.Address, version),
            _ => throw Access.Missing()
        };
    }
    public async Task<MasterView> GetAsync(string resource, Guid id, CancellationToken ct) => View(await Load(resource, id, ct));
    async Task References(string resource, MasterInput value, CancellationToken ct)
    {
        if (resource == "warehouses" && !await db.Branches.AnyAsync(x => x.Id == value.BranchId && x.IsActive, ct)) throw Access.Invalid("An active branch is required.");
        if (resource == "products" && (!await db.Categories.AnyAsync(x => x.Id == value.CategoryId && x.IsActive, ct)
            || !await db.Units.AnyAsync(x => x.Id == value.UnitId && x.IsActive, ct))) throw Access.Invalid("An active category and unit are required.");
    }
    public async Task<MasterView> CreateAsync(string resource, MasterInput value, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await access.RequireRole(ct); await References(resource, value, ct);
        var now = DateTime.UtcNow;
        BaseEntity entity = resource switch
        {
            "branches" => new Branch(value.Code!, value.Name, now), "warehouses" => new Warehouse(value.BranchId!.Value, value.Code!, value.Name, now),
            "categories" => new Category(value.Name, now), "units" => new Unit(value.Code!, value.Name, now),
            "products" => new Product(value.Code!, value.Name, value.CategoryId!.Value, value.UnitId!.Value, now),
            "suppliers" => new Supplier(value.Code!, value.Name, now, value.Email, value.Phone, value.Address), _ => throw Access.Missing()
        };
        db.Add(entity); db.Audit(access.Actor, "created", resource, entity.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
    async Task<bool> ProductUsed(Guid id, CancellationToken ct) => await db.PurchaseOrderLines.AnyAsync(x => x.ProductId == id, ct)
        || await db.StockOpeningLines.AnyAsync(x => x.ProductId == id, ct) || await db.StockReceiptLines.AnyAsync(x => x.ProductId == id, ct)
        || await db.StockIssueLines.AnyAsync(x => x.ProductId == id, ct) || await db.StockTransferLines.AnyAsync(x => x.ProductId == id, ct)
        || await db.StockMovements.AnyAsync(x => x.ProductId == id, ct);
    public async Task<MasterView> UpdateAsync(string resource, Guid id, MasterInput value, byte[] version, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await access.RequireRole(ct);
        var entity = await Load(resource, id, ct); Access.Match(version, db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        await References(resource, value, ct); var now = DateTime.UtcNow;
        switch (entity)
        {
            case Branch x: x.UpdateDetails(value.Code!, value.Name, now); break;
            case Warehouse x:
                if (x.BranchId != value.BranchId) throw Access.Invalid("Warehouse branch cannot be changed.");
                x.UpdateDetails(value.Code!, value.Name, now); break;
            case Category x: x.Rename(value.Name, now); break;
            case Unit x: x.UpdateDetails(value.Code!, value.Name, now); break;
            case Product x:
                if (x.UnitId != value.UnitId && await ProductUsed(id, ct)) throw Access.Conflict("product_used", "A used product's base unit cannot be changed.");
                x.UpdateDetails(value.Code!, value.Name, value.CategoryId!.Value, now); x.ChangeBaseUnit(value.UnitId!.Value, now); break;
            case Supplier x: x.UpdateDetails(value.Code!, value.Name, now, value.Email, value.Phone, value.Address); break;
        }
        db.Audit(access.Actor, "updated", resource, id); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
    public async Task<MasterView> SetActiveAsync(string resource, Guid id, bool active, byte[] version, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await access.RequireRole(ct);
        var entity = await Load(resource, id, ct); Access.Match(version, db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        if (active) entity.Activate(DateTime.UtcNow); else entity.Deactivate(DateTime.UtcNow);
        db.Audit(access.Actor, active ? "activated" : "deactivated", resource, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
}
