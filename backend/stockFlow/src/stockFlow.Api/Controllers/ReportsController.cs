using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/reports")]
public sealed class ReportsController(IReportService reports) : FeatureController
{
    [HttpGet("balances")] public Task<PagedResult<BalanceView>> Balances([FromQuery] PageRequest page, Guid? warehouseId, Guid? productId, CancellationToken ct) => reports.BalancesAsync(page, warehouseId, productId, ct);
    [HttpGet("movements")] public Task<PagedResult<MovementView>> Movements([FromQuery] PageRequest page, Guid? warehouseId, Guid? productId, DateTime? from, DateTime? to, CancellationToken ct) => reports.MovementsAsync(page, warehouseId, productId, from, to, ct);
    [HttpGet("reconciliation")] public Task<PagedResult<ReconciliationView>> Reconcile([FromQuery] PageRequest page, Guid? warehouseId, CancellationToken ct) => reports.ReconcileAsync(page, warehouseId, ct);
    [HttpGet("dashboard")] public Task<DashboardView> Dashboard(CancellationToken ct) => reports.DashboardAsync(ct);
    [Authorize(Roles = Roles.Admin), HttpGet("audit")] public Task<PagedResult<AuditView>> Audit([FromQuery] PageRequest page, CancellationToken ct) => reports.AuditAsync(page, ct);
}
