# StockFlow MVP Requirements

## Document status

- Phase: 00 — requirements analysis and documentation
- Status: Drafted from the Phase 00 scope; business decisions are not yet confirmed by stakeholders.
- Scope: A single company with multiple branches and warehouses.
- Implementation evidence: The repository currently contains only a minimal .NET 10 web host. No MVP feature is verified as implemented.

## Product goal

StockFlow shall provide a controlled inventory workflow for one company operating multiple branches and warehouses. Authorized users must be able to maintain shared catalog data, record opening stock, receive complete purchase orders, issue stock, transfer stock immediately between warehouses, and inspect balances, immutable movements, and business audit history.

## MVP in scope

### Identity and access

- Authenticate users.
- Allow administrators to create, deactivate, and manage users and their roles.
- Assign users to warehouses through explicit warehouse assignments.
- Enforce role permissions and warehouse scope on lists, details, and mutations.
- Provide the roles Admin, PurchasingOfficer, WarehouseKeeper, and Viewer.
- Allow shared catalog access as defined by the access matrix; catalog access is not implicitly limited to a warehouse.

### Organization and master data

- Maintain branches and their warehouses.
- Maintain categories, units, products, and suppliers.
- Each product has exactly one base unit in the MVP.
- Products, suppliers, categories, units, branches, and warehouses are deactivated rather than deleted when referenced by business records.
- Normalize unique codes and SKUs consistently, including case and surrounding whitespace, before comparison.

### Inventory documents

- Create and post opening-stock documents.
- Create, submit, and receive purchase orders.
- Receive a purchase order in full through exactly one receipt record.
- Create and post stock issues; every issue requires a reason.
- Create and post immediate transfers between two warehouses.
- Expose current stock balances and immutable signed stock movements.
- Record business audit events for security-sensitive and stock-affecting actions.

## Explicitly out of scope

- Sales and customer management.
- Accounting, payments, and inventory valuation.
- Taxes and discounts.
- Partial receiving and purchase returns.
- Multiple currencies and unit conversions.
- In-transit transfer workflows.
- Multi-tenancy.
- External integrations.

## Functional requirements

| ID | Requirement | Acceptance intent |
|---|---|---|
| FR-01 | Authentication | An unauthenticated user cannot access protected MVP operations. |
| FR-02 | User administration | An Admin can manage users, roles, and warehouse assignments. |
| FR-03 | Organization | The company can manage branches and warehouses. |
| FR-04 | Catalog | Authorized users can manage categories, units, products, and suppliers while preserving referenced records. |
| FR-05 | Opening stock | A WarehouseKeeper can post opening stock only for a product/warehouse pair with no prior movement. |
| FR-06 | Purchasing | A PurchasingOfficer can create and submit purchase orders for assigned warehouses and receive them in full. |
| FR-07 | Issues | A WarehouseKeeper can post an issue with a required reason when sufficient stock exists. |
| FR-08 | Transfers | A WarehouseKeeper can post an immediate transfer only when authorized for both source and destination warehouses. |
| FR-09 | Balances | The system exposes balances derived from atomic, signed movement effects. |
| FR-10 | Audit | Business audit records identify the actor, action, target, time, outcome, and relevant request/correlation context. |

## Document lifecycle

- Purchase order: `Draft -> Submitted -> Received`.
- Purchase-order cancellation is allowed before receipt posting; cancellation is not a receipt state.
- Only `Draft` purchase orders can be edited.
- Stock documents (opening stock, issue, transfer, and receipt posting records): `Draft -> Posted` or `Draft -> Cancelled`.
- A posted document and its movements cannot be edited or deleted.
- An unposted cancelled receipt may be reopened only through an explicit authorized workflow, returning it to `Draft`; once posted, it cannot be reopened.

## Cross-cutting requirements

- All quantities are positive at input; movement direction supplies the sign.
- Purchase prices are nonnegative.
- Duplicate products within one document are rejected.
- Posting is the only operation that changes stock.
- Posting is idempotent: retrying a completed post cannot apply stock effects twice.
- A stock operation that would make any balance negative is rejected as a whole.
- Balances, movements, document state, and the related business audit event commit atomically.
- Concurrent operations use optimistic concurrency plus database transaction/locking behavior sufficient to prevent negative stock and duplicate posting.
- API clients must treat concurrency and precondition failures as retryable only after reloading current state; clients must not blindly replay a non-idempotent request without its idempotency/precondition contract.

## Proposed API behavior

These are proposed defaults for later API design, not implemented contracts: protected endpoints return `401` for missing authentication, `403` for authenticated users outside the required role or warehouse scope, `404` when a resource is not visible in the caller's scope, `409` for state/concurrency/duplicate-posting conflicts, and `422` for business validation failures. The exact endpoint names, DTOs, ETags, and idempotency-key policy remain unresolved until Phase 01.

## Acceptance scenarios

The canonical stock scenario is: opening stock posts 10 units of product P in warehouse A; a full receipt posts 5 units in A; an issue posts 3 units in A; a transfer posts 4 units from A to B. The expected result is A = 8, B = 4, total = 12. See `business-rules.md` for Given/When/Then criteria covering this and failure cases.

## Non-functional requirements

- Preserve an immutable movement history suitable for reconciliation.
- Use UTC timestamps and a consistent normalized representation for unique codes.
- Reject values outside supported precision/range rather than silently rounding or truncating them.
- Verify transaction and concurrency behavior using an isolated real SQL Server test database; never use a deployed database for automated tests.
- Do not expose secrets, private connection strings, signing keys, or deployment profiles in committed configuration.

## Unresolved decisions

1. Authentication protocol and token/session implementation.
2. Whether Admin is global by company or also represented by warehouse assignments.
3. Exact catalog actions allowed to PurchasingOfficer, WarehouseKeeper, and Viewer.
4. Branch-to-warehouse cardinality and whether a warehouse may move branches.
5. Receipt cancellation/reopen endpoint semantics before posting.
6. API pagination, filtering, ETag format, and idempotency-key requirements.
7. Audit retention, privacy/redaction, and who may read audit logs.
8. Whether negative adjustments are ever needed outside issues and transfers (currently excluded).
