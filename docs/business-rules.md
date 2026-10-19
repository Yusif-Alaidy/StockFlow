# StockFlow MVP Business Rules

## Status and scope

This document defines the requested Phase 00 rules for a single company with multiple branches and warehouses. It is drafted, not stakeholder-confirmed, and is not evidence that the current application implements these rules.

## Roles, permissions, and scope

Warehouse scope applies to lists, details, and mutations. A user must be authenticated and must satisfy both the action permission and the required warehouse scope.

| Action area | Admin | PurchasingOfficer | WarehouseKeeper | Viewer |
|---|---:|---:|---:|---:|
| User, role, warehouse-assignment administration | All | No | No | No |
| Branches, warehouses, categories, units, products, suppliers | All administrative operations | Shared catalog access as granted; no organization administration | Shared catalog access as granted; no organization administration | Read-only shared catalog access |
| Purchase orders: create/edit/submit | All warehouses | Assigned warehouses | No | Read-only in assigned warehouses |
| Purchase receiving | All warehouses | Assigned warehouses | No | Read-only in assigned warehouses |
| Opening-stock documents | All warehouses | No | Assigned warehouses | Read-only in assigned warehouses |
| Stock issues | All warehouses | No | Assigned warehouses | Read-only in assigned warehouses |
| Immediate transfers | Any source and destination | No | Assigned to both source and destination | Read-only only when both warehouses are in scope |
| Balances and movement history | All warehouses | Assigned warehouses | Assigned warehouses | Assigned warehouses, read-only |
| Business audit logs | Administrative access | No by default | No by default | No by default |

**Shared catalog access** is separate from warehouse scope because categories, units, products, and suppliers are company-level master data. The final per-role catalog write permissions remain an unresolved decision; authorization must still be enforced server-side.

Transfers require access to both warehouses. No caller may infer access to a destination merely from access to the source. Admin is not restricted by warehouse scope for administrative operations.

## General invariants

1. Quantities submitted to documents are strictly positive. Movement signs are assigned by the posting operation.
2. Purchase unit prices are nonnegative.
3. A document cannot contain the same product more than once.
4. A product has one base unit in the MVP; no conversion is permitted.
5. Only posting changes stock.
6. Posted documents and movement rows are immutable and cannot be deleted.
7. A retry of a successful posting has no additional stock effect.
8. Stock cannot become negative.
9. Referenced master data is deactivated rather than deleted.
10. Stock balances, movement rows, document state, and the corresponding business audit record commit in one transaction.

## Purchase orders and receiving

- A purchase order starts in `Draft`, can be edited only in `Draft`, and transitions to `Submitted`.
- A submitted order cannot be edited. Cancellation is allowed before receipt posting; a cancelled order cannot be received.
- Receipt lines are copied from the submitted PO. The receiver cannot add products, remove products, or override quantities.
- Receiving is full only: every submitted PO line is received at its submitted quantity exactly once.
- There is one receipt record per purchase order. A second receipt or repeated posting is rejected or treated as an idempotent retry without additional effects.
- Successful receipt posting transitions the PO to `Received` and posts its inbound movements atomically.
- An unposted cancelled receipt may be reopened by an authorized workflow, returning it to `Draft`; a posted receipt cannot be reopened, edited, or deleted.
- Purchase prices are nonnegative and line totals use the proposed calculation in `data-model.md`.

## Stock documents

Opening stock, issues, transfers, and receipt posting records use `Draft -> Posted` or `Draft -> Cancelled`.

- Opening stock is allowed only when the product/warehouse pair has no previous movement of any kind. Existing balance alone is not the test; movement history is authoritative.
- An issue requires a non-empty reason and sufficient available stock for every line. If any line fails, the entire document is rejected and no line is posted.
- A transfer is immediate, has one source and one destination warehouse, and requires source quantity. Source and destination effects are posted atomically. A transfer to the same warehouse is invalid.
- A cancelled or posted document cannot be edited. A posted document cannot be deleted.
- Posting a document more than once cannot duplicate movements or balances; a stable document/posting identity must be used to enforce this.

## Atomicity and concurrency

The posting transaction must validate current state, apply all balance changes, append immutable movements, update document state, and append the business audit event before commit. Failure rolls back every effect.

Optimistic concurrency must detect stale document or balance versions. Database concurrency protection must serialize conflicting balance updates sufficiently that two concurrent issues cannot both spend the same stock. A concurrency conflict returns a retryable conflict response after the client reloads state; an insufficient-stock result is a business rejection, not a blind retry.

## Acceptance criteria (Given/When/Then)

### Canonical stock flow

**Given** product P has no movements and warehouse A is assigned to the operator  
**When** 10 units of P are posted as opening stock in A  
**Then** A has 10 units and one immutable inbound movement exists.

**Given** a submitted PO for 5 units of P into A  
**When** the full receipt is posted  
**Then** A increases by 5, the PO becomes `Received`, and one receipt exists.

**Given** A has 15 units after opening and receipt  
**When** an issue of 3 units with reason `Production use` is posted  
**Then** A has 12 units and one immutable outbound movement exists.

**Given** A has 12 units and B has zero units  
**When** 4 units are transferred from A to B  
**Then** A = 8, B = 4, and total = 12.

### Integrity and failure behavior

**Given** a document contains a line that would make a balance negative  
**When** posting is attempted  
**Then** the entire document is rejected, no balance changes, no movements are appended, and no posted state is committed.

**Given** a document is already posted  
**When** the same post command is retried  
**Then** no additional movement or balance effect is created.

**Given** two concurrent issues compete for the same available stock  
**When** both try to commit  
**Then** at most the available stock is issued, at least one conflicting or insufficient operation fails, and no balance is negative.

**Given** a transfer fails during validation or transaction commit  
**When** the operation completes  
**Then** both source and destination balances remain unchanged and no half-transfer is visible.

**Given** an authenticated user lacks scope for warehouse W  
**When** the user requests a list, detail, or mutation involving W  
**Then** access is denied without leaking unauthorized resource details.

**Given** a PO already has a receipt record or has reached `Received`  
**When** receiving is requested again  
**Then** duplicate receiving is prevented and stock is unchanged.

**Given** a document is `Posted`  
**When** an edit or delete is requested  
**Then** the request is rejected and the document and movements remain unchanged.

**Given** balances and signed movement totals are queried for a warehouse/product  
**When** reconciliation runs  
**Then** the balance equals opening quantity plus all signed movements, subject to the defined opening convention, or the discrepancy is reported.

## Unresolved decisions

- Exact transition command names and authorization details for reopening an unposted cancelled receipt.
- Whether cancellation is represented as a PO state, a separate flag, or both.
- Isolation level and SQL Server locking strategy to be selected and proven in Phase 01/implementation tests.
- Audit event taxonomy and retention policy.
