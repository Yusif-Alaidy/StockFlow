# StockFlow Backlog

## Planning status

This backlog is a Phase 00 planning artifact. The MVP items below are proposed implementation work, not completed work. Dependencies are ordered so the team can complete only the next approved phase.

## MVP phases

### Phase 00 — Requirements and documentation

**Goal:** Define the MVP scope, business rules, data model, acceptance criteria, and unresolved decisions.  
**Dependencies:** None.  
**Status:** Documentation drafted; stakeholder decisions are not confirmed.

**Acceptance criteria:**

- Requirements, exclusions, roles, scope, states, integrity rules, and testable scenarios are documented.
- The data model includes the requested entities, constraints, Identity separation, refresh sessions, and accurate movement references.
- Progress records evidence, blockers, and the next recommended step.

### Phase 01 — Solution and architecture setup

**Goal:** Review the existing minimal solution and establish only the missing backend project structure and architectural boundaries for Domain, Application, Infrastructure, and API, without implementing the full inventory feature set. Confirm the chosen authentication, persistence, and API conventions.

**Dependencies:** Phase 00 decisions reviewed; existing `backend/stockFlow/` structure inspected.

**Acceptance criteria:**

- Existing solution/projects are reviewed rather than recreated.
- Proposed Domain/Application/Infrastructure boundaries preserve Domain independence from EF Core, Identity, ASP.NET, and Infrastructure.
- Authentication/session, SQL Server/EF Core, concurrency, error, and API conventions are recorded and approved.
- Any setup code is covered by relevant build checks; no Phase 02 feature is started.

### Phase 02 — Identity, organization, and shared catalog

**Goal:** Implement authentication, Admin-managed users, roles, warehouse assignments, branches, warehouses, categories, units, products, and suppliers.

**Dependencies:** Phase 01 architecture and persistence setup.

**Acceptance criteria:**

- Authentication and role/warehouse authorization work server-side.
- Scope applies to lists, details, and mutations; transfers later require both warehouses.
- Shared catalog access is separate from warehouse scope.
- Referenced master records can be deactivated but not deleted.
- Relevant tests run without using a deployed database.

### Phase 03 — Opening stock and stock foundation

**Goal:** Implement opening-stock documents, StockBalance, immutable StockMovement, posting, reconciliation, and business audit logging.

**Dependencies:** Phase 02 organization/catalog; Phase 01 transaction/concurrency decisions.

**Acceptance criteria:**

- Opening stock is allowed only for a product/warehouse pair with no previous movements.
- Posting is atomic, idempotent, immutable after posting, and cannot create negative stock.
- Balance equals signed movement totals under the documented opening convention.
- Concurrency behavior is verified against an isolated real SQL Server test database.

### Phase 04 — Purchasing and full receiving

**Goal:** Implement purchase orders, submission/cancellation, one full receipt per PO, copied receipt lines, totals, and audit events.

**Dependencies:** Phase 03 posting/movement foundation and Phase 02 suppliers/warehouses/products.

**Acceptance criteria:**

- PO lifecycle is Draft -> Submitted -> Received; only Draft is editable.
- Cancellation is allowed before receipt posting.
- Receiving is full only, copies submitted lines, and cannot be repeated.
- Receipt posting changes stock and state atomically.

### Phase 05 — Issues and immediate transfers

**Goal:** Implement reasoned stock issues and immediate two-warehouse transfers.

**Dependencies:** Phase 03 stock foundation; Phase 02 authorization; Phase 04 posting patterns.

**Acceptance criteria:**

- Issues require a reason and reject the whole document if stock is insufficient.
- Transfers require access to both warehouses and update both balances atomically.
- Failed transfers leave both warehouses unchanged.
- Concurrent operations cannot create negative balances.

### Phase 06 — API hardening and MVP verification

**Goal:** Complete API metadata and feature documentation, error/precondition/retry behavior, reconciliation checks, audit review, and end-to-end MVP verification.

**Dependencies:** Phases 02–05 implemented.

**Acceptance criteria:**

- Each endpoint documents permissions, warehouse scope, DTO validation, success/error examples, and ETag/precondition/retry behavior where applicable.
- Acceptance scenarios in `business-rules.md` pass.
- SQL Server transaction/concurrency tests use an isolated test database and do not target deployment.
- Build, tests, and documentation checks have recorded evidence.

## Future enhancements (not MVP)

- Sales and customers.
- Accounting, payments, taxes, discounts, and inventory valuation.
- Partial receiving and purchase returns.
- Multiple currencies and unit conversions.
- In-transit transfers.
- Multi-tenancy.
- External integrations.
- Reporting, forecasting, barcode/scanning, and advanced inventory policies after MVP decisions.

## Cross-phase unresolved decisions

Authentication protocol, exact catalog write matrix, branch/warehouse ownership rules, receipt reopen semantics, API resource naming, idempotency/ETag format, audit retention, and SQL Server concurrency strategy must be resolved before the phases that depend on them. See the requirements, business-rules, and data-model documents.
