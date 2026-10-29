using Microsoft.EntityFrameworkCore;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
using stockFlow.Domain.Entities;
using stockFlow.Domain.Enums;
using stockFlow.Infrastructure.Persistence;
namespace stockFlow.Infrastructure.Features;
internal sealed class DocumentService(StockFlowDbContext db, Access access, DocumentNumberGenerator numbers) : IDocumentService
{
    static readonly string[] Kinds = ["purchase-orders", "openings", "receipts", "issues", "transfers"];
    async Task Permission(string kind, CancellationToken ct)
    {
        if (!Kinds.Contains(kind)) throw Access.Missing();
        await access.RequireRole(ct, kind is "purchase-orders" or "receipts" ? Roles.PurchasingOfficer : Roles.WarehouseKeeper);
    }
    async Task Scope(DocumentView view, CancellationToken ct)
    {
        await access.Warehouse(view.WarehouseId, ct, true);
        if (view.DestinationWarehouseId is Guid target) await access.Warehouse(target, ct, true);
    }
    async Task References(DocumentView view, CancellationToken ct)
    {
        var ids = view.DestinationWarehouseId is Guid target ? new[] { view.WarehouseId, target } : new[] { view.WarehouseId };
        if (await db.Warehouses.CountAsync(w => ids.Contains(w.Id) && w.IsActive && db.Branches.Any(b => b.Id == w.BranchId && b.IsActive), ct) != ids.Length)
            throw Access.Invalid("All warehouses and their branches must be active.");
        if (view.SupplierId is Guid supplier && !await db.Suppliers.AnyAsync(x => x.Id == supplier && x.IsActive, ct)) throw Access.Invalid("Supplier must be active.");
        var products = view.Lines.Select(x => x.ProductId).ToArray();
        if (products.Length == 0 || products.Length > 200 || products.Distinct().Count() != products.Length) throw Access.Invalid("Use 1–200 unique product lines.");
        if (await db.Products.CountAsync(p => products.Contains(p.Id) && p.IsActive
            && db.Units.Any(u => u.Id == p.UnitId && u.IsActive) && db.Categories.Any(c => c.Id == p.CategoryId && c.IsActive), ct) != products.Length)
            throw Access.Invalid("Each product, category and base unit must be active.");
    }
    public Task<PagedResult<DocumentView>> ListAsync(string kind, PageRequest page, Guid? warehouseId, string? status, CancellationToken ct) => kind switch
    {
        "purchase-orders" => List(db.PurchaseOrders.Include(x => x.Lines), kind, page, warehouseId, status, ct),
        "openings" => List(db.StockOpenings.Include(x => x.Lines), kind, page, warehouseId, status, ct),
        "receipts" => List(db.StockReceipts.Include(x => x.Lines), kind, page, warehouseId, status, ct),
        "issues" => List(db.StockIssues.Include(x => x.Lines), kind, page, warehouseId, status, ct),
        "transfers" => List(db.StockTransfers.Include(x => x.Lines), kind, page, warehouseId, status, ct), _ => throw Access.Missing()
    };
    async Task<PagedResult<DocumentView>> List<T>(IQueryable<T> query, string kind, PageRequest page, Guid? warehouseId, string? status, CancellationToken ct) where T : class
    {
        var skip = page.GetSkip(); var allowed = await access.Warehouses(ct);
        var field = kind == "transfers" ? "FromWarehouseId" : "WarehouseId";
        if (warehouseId is Guid requested) { await access.Warehouse(requested, ct); query = query.Where(x => EF.Property<Guid>(x, field) == requested); }
        if (allowed is not null)
        {
            query = query.Where(x => allowed.Contains(EF.Property<Guid>(x, field)));
            if (kind == "transfers") query = query.Where(x => allowed.Contains(EF.Property<Guid>(x, "ToWarehouseId")));
        }
        if (status is not null)
        {
            if (kind == "purchase-orders")
            {
                if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var state) || !Enum.IsDefined(state)) throw Access.Invalid("Invalid status.");
                query = query.Where(x => EF.Property<PurchaseOrderStatus>(x, "Status") == state);
            }
            else
            {
                if (!Enum.TryParse<StockDocumentStatus>(status, true, out var state) || !Enum.IsDefined(state)) throw Access.Invalid("Invalid status.");
                query = query.Where(x => EF.Property<StockDocumentStatus>(x, "Status") == state);
            }
        }
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => EF.Property<DateTime>(x, "CreatedAt")).ThenBy(x => EF.Property<Guid>(x, "Id")).Skip(skip).Take(page.PageSize).ToListAsync(ct);
        return new(rows.Select(x => View(x)).ToList(), page.PageNumber, page.PageSize, total);
    }
    async Task<object> Load(string kind, Guid id, CancellationToken ct)
    {
        object? entity = kind switch
        {
            "purchase-orders" => await db.PurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct),
            "openings" => await db.StockOpenings.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct),
            "receipts" => await db.StockReceipts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct),
            "issues" => await db.StockIssues.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct),
            "transfers" => await db.StockTransfers.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct), _ => null
        };
        if (entity is null) throw Access.Missing(); await Scope(View(entity), ct); return entity;
    }
    public async Task<DocumentView> GetAsync(string kind, Guid id, CancellationToken ct) => View(await Load(kind, id, ct));
    static DocumentView View(object entity) => entity switch
    {
        PurchaseOrder x => new(x.Id, x.Number, "purchase-orders", x.Status.ToString(), x.WarehouseId, null, x.SupplierId, null, null, x.Notes, x.CreatedAt, x.Lines.Select(l => new DocumentLineView(l.Id, l.ProductId, l.Quantity, l.UnitPrice)).ToList(), Convert.ToBase64String(x.RowVersion)),
        StockOpening x => new(x.Id, x.Number, "openings", x.Status.ToString(), x.WarehouseId, null, null, null, null, x.Notes, x.CreatedAt, x.Lines.Select(l => new DocumentLineView(l.Id, l.ProductId, l.Quantity, 0)).ToList(), Convert.ToBase64String(x.RowVersion)),
        StockReceipt x => new(x.Id, x.Number, "receipts", x.Status.ToString(), x.WarehouseId, null, null, x.PurchaseOrderId, null, x.Notes, x.CreatedAt, x.Lines.Select(l => new DocumentLineView(l.Id, l.ProductId, l.Quantity, l.UnitPrice)).ToList(), Convert.ToBase64String(x.RowVersion)),
        StockIssue x => new(x.Id, x.Number, "issues", x.Status.ToString(), x.WarehouseId, null, null, null, x.Reason, x.Notes, x.CreatedAt, x.Lines.Select(l => new DocumentLineView(l.Id, l.ProductId, l.Quantity, 0)).ToList(), Convert.ToBase64String(x.RowVersion)),
        StockTransfer x => new(x.Id, x.Number, "transfers", x.Status.ToString(), x.FromWarehouseId, x.ToWarehouseId, null, null, null, x.Notes, x.CreatedAt, x.Lines.Select(l => new DocumentLineView(l.Id, l.ProductId, l.Quantity, 0)).ToList(), Convert.ToBase64String(x.RowVersion)),
        _ => throw Access.Missing()
    };
    static void ValidateLines(DocumentLineInput[] lines, bool priced)
    {
        if (lines.Length < 1 || lines.Length > 200 || lines.Select(x => x.ProductId).Distinct().Count() != lines.Length) throw Access.Invalid("Use 1–200 unique product lines.");
        if (!priced && lines.Any(x => x.UnitPrice != 0)) throw Access.Invalid("Unit price is only accepted on purchase orders.");
    }
    public async Task<DocumentView> CreateAsync(string kind, DocumentInput request, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await Permission(kind, ct);
        if (kind == "receipts") throw Access.Invalid("Use the receipt-from-order endpoint.");
        ValidateLines(request.Lines, kind == "purchase-orders");
        await access.Warehouse(request.WarehouseId, ct);
        if (kind == "transfers") await access.Warehouse(request.DestinationWarehouseId ?? Guid.Empty, ct);
        var now = DateTime.UtcNow; var actor = access.Actor;
        object entity;
        switch (kind)
        {
            case "purchase-orders":
                var order = new PurchaseOrder(await numbers.NextPurchaseOrderAsync(ct), request.SupplierId ?? Guid.Empty, request.WarehouseId, actor, now, request.Notes);
                foreach (var line in request.Lines) order.AddLine(line.ProductId, line.Quantity, line.UnitPrice, now); entity = order; break;
            case "openings":
                var opening = new StockOpening(await numbers.NextOpeningAsync(ct), request.WarehouseId, actor, now, request.Notes);
                foreach (var line in request.Lines) opening.AddLine(line.ProductId, line.Quantity, now); entity = opening; break;
            case "issues":
                var issue = new StockIssue(await numbers.NextIssueAsync(ct), request.WarehouseId, request.Reason!, actor, now, request.Notes);
                foreach (var line in request.Lines) issue.AddLine(line.ProductId, line.Quantity, now); entity = issue; break;
            case "transfers":
                var transfer = new StockTransfer(await numbers.NextTransferAsync(ct), request.WarehouseId, request.DestinationWarehouseId ?? Guid.Empty, actor, now, request.Notes);
                foreach (var line in request.Lines) transfer.AddLine(line.ProductId, line.Quantity, now); entity = transfer; break;
            default: throw Access.Missing();
        }
        await References(View(entity), ct); db.Add(entity); db.Audit(actor, "created", kind, View(entity).Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
    public async Task<DocumentView> ReceiveAsync(ReceiptInput request, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await Permission("receipts", ct);
        var order = (PurchaseOrder)await Load("purchase-orders", request.PurchaseOrderId, ct);
        var existing = await db.StockReceipts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.PurchaseOrderId == order.Id, ct);
        if (existing is not null) { await tx.CommitAsync(ct); return View(existing); }
        var receipt = StockReceipt.FromPurchaseOrder(await numbers.NextReceiptAsync(ct), order, access.Actor, DateTime.UtcNow, request.Notes);
        await References(View(receipt), ct); db.StockReceipts.Add(receipt); db.Audit(access.Actor, "created", "receipts", receipt.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(receipt);
    }
    public async Task<DocumentView> EditAsync(string kind, Guid id, DocumentEdit request, byte[] version, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await Permission(kind, ct);
        var entity = await Load(kind, id, ct); var view = View(entity); Access.Match(version, Convert.FromBase64String(view.Version));
        if (kind == "receipts") throw Access.Invalid("Receipt lines are immutable order snapshots. Cancel/reopen instead.");
        ValidateLines(request.Lines, kind == "purchase-orders"); var now = DateTime.UtcNow;
        switch (entity)
        {
            case PurchaseOrder x:
                x.UpdateNotes(request.Notes, now);
                foreach (var old in x.Lines.ToArray()) { x.RemoveLine(old.Id, now); db.Remove(old); }
                foreach (var l in request.Lines) x.AddLine(l.ProductId, l.Quantity, l.UnitPrice, now);
                break;
            case StockOpening x:
                x.UpdateNotes(request.Notes, now);
                foreach (var old in x.Lines.ToArray()) { x.RemoveLine(old.Id, now); db.Remove(old); }
                foreach (var l in request.Lines) x.AddLine(l.ProductId, l.Quantity, now);
                break;
            case StockIssue x:
                x.UpdateNotes(request.Notes, now);
                foreach (var old in x.Lines.ToArray()) { x.RemoveLine(old.Id, now); db.Remove(old); }
                foreach (var l in request.Lines) x.AddLine(l.ProductId, l.Quantity, now);
                break;
            case StockTransfer x:
                x.UpdateNotes(request.Notes, now);
                foreach (var old in x.Lines.ToArray()) { x.RemoveLine(old.Id, now); db.Remove(old); }
                foreach (var l in request.Lines) x.AddLine(l.ProductId, l.Quantity, now);
                break;
        }
        await References(View(entity), ct); db.Audit(access.Actor, "updated", kind, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
    public async Task<DocumentView> ActAsync(string kind, Guid id, string action, byte[] version, CancellationToken ct)
    {
        await using var tx = await WriteTransaction.Begin(db, ct); await Permission(kind, ct);
        var entity = await Load(kind, id, ct); var view = View(entity);
        // A retry of a completed post is a read of the same result, never a second stock change.
        if (action == "post" && view.Status == "Posted") { await tx.CommitAsync(ct); return view; }
        Access.Match(version, Convert.FromBase64String(view.Version));
        var actor = access.Actor; var now = DateTime.UtcNow;
        switch (action)
        {
            case "submit" when entity is PurchaseOrder order:
                await References(view, ct); order.Submit(actor, now); break;
            case "reopen" when entity is StockReceipt receipt:
                var source = await db.PurchaseOrders.SingleAsync(x => x.Id == receipt.PurchaseOrderId, ct);
                if (source.Status != PurchaseOrderStatus.Submitted) throw Access.Conflict("order_state", "Order must be submitted.");
                receipt.Reopen(actor, now); break;
            case "cancel":
                switch (entity)
                {
                    case PurchaseOrder x: x.Cancel(actor, now); break;
                    case StockOpening x: x.Cancel(actor, now); break;
                    case StockReceipt x: x.Cancel(actor, now); break;
                    case StockIssue x: x.Cancel(actor, now); break;
                    case StockTransfer x: x.Cancel(actor, now); break;
                }
                break;
            case "post" when entity is not PurchaseOrder:
                if (view.Status != "Draft") throw Access.Conflict("document_state", "Only drafts can be posted.");
                await References(view, ct);
                if (entity is StockReceipt r)
                {
                    var po = await db.PurchaseOrders.SingleAsync(x => x.Id == r.PurchaseOrderId, ct);
                    if (!await db.Suppliers.AnyAsync(x => x.Id == po.SupplierId && x.IsActive, ct)) throw Access.Invalid("Supplier must be active.");
                    po.MarkReceived(actor, now);
                }
                foreach (var line in view.Lines.OrderBy(x => x.ProductId))
                {
                    if (entity is StockOpening && await db.StockMovements.AnyAsync(x => x.WarehouseId == view.WarehouseId && x.ProductId == line.ProductId, ct))
                        throw Access.Conflict("opening_exists", "Opening is only allowed before any movement for a warehouse/product pair.");
                    var type = entity switch { StockOpening => StockDocumentType.Opening, StockReceipt => StockDocumentType.Receipt, StockIssue => StockDocumentType.Issue, _ => StockDocumentType.Transfer };
                    var delta = entity is StockIssue or StockTransfer ? -line.Quantity : line.Quantity;
                    await Move(view.WarehouseId, line, delta, type, id, actor, now, ct);
                    if (view.DestinationWarehouseId is Guid destination) await Move(destination, line, line.Quantity, type, id, actor, now, ct);
                }
                switch (entity)
                {
                    case StockOpening x: x.Post(actor, now); break;
                    case StockReceipt x: x.Post(actor, now); break;
                    case StockIssue x: x.Post(actor, now); break;
                    case StockTransfer x: x.Post(actor, now); break;
                }
                break;
            default: throw Access.Invalid("Action is not supported for this document type.");
        }
        db.Audit(actor, action, kind, id); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View(entity);
    }
    async Task Move(Guid warehouse, DocumentLineView line, decimal delta, StockDocumentType type, Guid document, Guid actor, DateTime now, CancellationToken ct)
    {
        var balance = await db.StockBalances.SingleOrDefaultAsync(x => x.WarehouseId == warehouse && x.ProductId == line.ProductId, ct);
        if (balance is null) { balance = new StockBalance(warehouse, line.ProductId); db.StockBalances.Add(balance); }
        if (balance.Quantity + delta < 0) throw Access.Conflict("insufficient_stock", "Posting would create negative stock.");
        balance.Apply(delta);
        db.StockMovements.Add(new StockMovement(type, document, line.Id, warehouse, line.ProductId, delta, actor, now));
    }
}
