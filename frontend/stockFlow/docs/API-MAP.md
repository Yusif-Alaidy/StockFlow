# API integration contract

Configured base: `https://stock-flow.runasp.net/api/v1`.
The deployed `/openapi/v1.json` was retrieved on 2026-10-28 and preserved as `backend-openapi.snapshot.json`. DTOs and routes were compared against that snapshot and the delivered C# controllers/contracts. No production business mutations were made.

| Screen | Routes relative to base |
| --- | --- |
| Login / session | POST auth/login, auth/refresh, auth/logout; GET auth/me |
| Account | POST auth/change-password |
| Master data | GET/POST branches, warehouses, categories, units, products, suppliers |
| Master detail/edit | GET/PUT {resource}/{id}; POST {resource}/{id}/activate or deactivate |
| Documents | GET/POST purchase-orders, openings, receipts, issues, transfers |
| Document detail | GET {kind}/{id} |
| Edit draft | PUT {kind}/{id}, except receipts |
| PO actions | POST purchase-orders/{id}/submit or cancel |
| Stock actions | POST {kind}/{id}/post or cancel |
| Receipt recovery | POST receipts/{id}/reopen |
| Users | GET/POST users; PUT users/{id} |
| Reports | GET reports/dashboard, balances, movements, reconciliation, audit |

## Payloads

Master input: name, code, branchId, categoryId, unitId, email, phone, address. Product SKU maps to `code`, not `sku`. Only relevant fields are populated; optional fields are null. Row view includes id, isActive and base64 version.

Document create: warehouseId, supplierId, destinationWarehouseId, reason, notes, lines[]. Each line is productId, quantity, unitPrice. Price is zero except on purchase orders. Edit sends only notes and lines; warehouse, supplier, destination and reason remain fixed.

Receipt create: purchaseOrderId and notes. The backend snapshots the full order lines. There is no editable receipt line payload.

List responses: items, pageNumber, pageSize, totalCount. Lists use page size 20. Lookup selectors retrieve all pages with page size 100, retaining inactive references for historical display. New records restrict selectable inactive masters where available; backend remains authoritative for active branch/category/unit dependencies.

Mutation concurrency: use the original DTO version as a quoted `If-Match` header. A 412 leaves the original edit unsaved and requires explicit reload; no silent overwrite. ETag header is not required to be readable because version is also present in DTO.

Token result: accessToken, accessExpiresAt, refreshToken, refreshExpiresAt, user. Opaque tokens are not parsed as JWT. Refresh is single-flight per tab. Only API 401 responses may trigger a single refresh/replay; no retry on 500, timeout, 409 or 412.

## Permissions and states

Admin: all operations; PurchasingOfficer: PO/receipts; WarehouseKeeper: openings/issues/transfers; Viewer: read. Users and audit screens are Admin-only. Master writes are Admin-only. Backend enforces warehouse scoping, including both warehouses on transfers.

PO: Draft -> Submitted -> Received; cancellation where backend rules permit. Stock document: Draft -> Posted or Cancelled. Cancelled receipts can reopen. Posted/received records expose no edit actions.

## Numbers and dates

Quantity > 0, up to 3 decimal places; PO price >= 0, up to 2. Duplicate product lines rejected before submission. Up to 200 lines. JavaScript-safe scaled numbers only are accepted by the form; very large SQL decimal values beyond that range require a future decimal-string strategy rather than silently losing precision. Values displayed using the product's own unit; no mixed-unit stock total.

Movement date filter explicitly uses UTC. Selected ending day becomes exclusive next-day UTC, matching backend `OccurredAt < to`. Dates are displayed in the browser's local timezone.

## Deliberate scope

Single company, multi warehouse MVP. No signup, forecasting, taxes/currency conversion, partial receipts, returns, barcode scanning, stock adjustments, attachments, notifications service, global search, fake statistics or exports without matching backend support. There is no client-side replacement for backend stock rules.
