<div align="center">

# StockFlow

### Inventory & Purchasing Management

An Arabic-first inventory workspace for managing purchasing, warehouse operations, and traceable stock movements.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/Database-SQL_Server-CC2927)
[![Clean Architecture](https://img.shields.io/badge/Clean_Architecture-176650)](docs/architecture.md)

[Frontend](https://stock-flow-ten-dun.vercel.app) · [API](https://stock-flow.runasp.net) · [Getting Started](#getting-started) · [Documentation](#documentation)

</div>

---

## Overview

StockFlow is a full-stack inventory and purchasing application built with **ASP.NET Core, Angular, and SQL Server**. It supports one company with multiple branches and warehouses, connecting purchase orders, receipts, opening balances, issues, and warehouse transfers through a stock ledger.

The project focuses on practical business rules: preventing negative stock, protecting posted documents from modification, restricting access by warehouse, and detecting concurrent edits before they overwrite another user's work.

The frontend provides an **Arabic RTL interface**, responsive layouts, and role-aware workflows backed by server-side authorization.

## Features

| Area | Capabilities |
| --- | --- |
| Organization | Manage branches and warehouses; activate or deactivate records while preserving historical references. |
| Catalog | Manage products, unique SKUs, categories, units, and suppliers. |
| Purchasing | Create and edit draft purchase orders, submit orders, and track their receipt status. |
| Receiving | Create a full receipt from a submitted purchase order and post it to stock. |
| Opening balances | Establish initial stock before any movement exists for a product/warehouse pair. |
| Stock issues | Record outgoing quantities with a reason and reject posting that would create negative stock. |
| Transfers | Move stock between different warehouses in one transaction. |
| Reporting | View stock balances, movement history, balance/ledger reconciliation, and dashboard counters. |
| Access management | Manage users, roles, active status, and warehouse assignments. |
| Audit | Record application operations for administrative review. |
| Frontend experience | Reactive forms, pagination, validation feedback, confirmation dialogs, and stale-edit handling. |

## Business Rules

- **Single company, multiple warehouses.** Branches group warehouses; stock belongs to a product/warehouse pair.
- **One full receipt per purchase order.** Receipt lines snapshot the order. A cancelled receipt can be reopened according to the order's state.
- **Posted stock documents are immutable.** Editing applies to drafts; posted documents cannot be changed through the normal editing flow.
- **Stock cannot become negative.** The backend checks availability when posting issues and transfers.
- **Posting is transactional.** Balance updates, stock movements, document state, and audit records are committed together.
- **Concurrent edits are checked.** Versioned updates and actions require a quoted `If-Match` value; stale versions return `412 Precondition Failed`.
- **Access is warehouse-scoped.** Transfers require access to both the source and destination warehouses.
- **Quantities and prices have defined precision.** Quantities use `decimal(18,3)`; purchase prices use `decimal(18,2)`. Quantities must be positive and prices non-negative.
- **Units remain meaningful.** The UI does not combine different products' units into one misleading stock total.

### Document lifecycle

| Document | Main flow |
| --- | --- |
| Purchase order | `Draft → Submitted → Received`; cancellation where business rules permit. |
| Opening, receipt, issue, transfer | `Draft → Posted` or `Draft → Cancelled`. |
| Cancelled receipt | `Cancelled → Draft` through the explicit reopen action, when allowed. |

## Technology Stack

| Layer | Technologies |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Web API, Entity Framework Core 10 |
| Database | Microsoft SQL Server, migrations, row versions, transactions |
| Frontend | Angular 21, TypeScript 5.9, RxJS, Reactive Forms, signals |
| Authentication | Opaque bearer session tokens, rotating refresh tokens, ASP.NET Core password hashing |
| API contract | OpenAPI, Swagger UI, ProblemDetails, ETag / If-Match |
| Backend tests | xUnit: unit, architecture, and SQL integration tests |
| Frontend tests | Vitest, Angular TestBed, HTTP testing utilities |
| Hosting | MonsterASP for the API/database; Vercel for the frontend |

## Architecture

The backend follows Clean Architecture with explicit project boundaries:

| Project | Responsibility | Project dependencies |
| --- | --- | --- |
| `stockFlow.Domain` | Entities, validation, document states, and business invariants. | No application or infrastructure project dependency. |
| `stockFlow.Application` | Use-case contracts, DTOs, and application abstractions. | Domain |
| `stockFlow.Infrastructure` | EF Core persistence, SQL transactions, authentication, and service implementations. | Application and Domain |
| `stockFlow.Api` | HTTP endpoints, authentication wiring, middleware, and application composition. | Application and Infrastructure |

The Angular client uses standalone components, feature screens, shared UI utilities, and services for API access and session management.

**Concurrency trade-off:** application writes currently share a transaction-owned SQL application lock. This makes stock updates easier to reason about for the MVP, while limiting write throughput. Higher write concurrency is a future improvement, not a claim of the current design.

## Repository Structure

| Path | Contents |
| --- | --- |
| `backend/stockFlow/stockFlow.slnx` | Backend solution |
| `backend/stockFlow/src/stockFlow.Domain/` | Domain layer |
| `backend/stockFlow/src/stockFlow.Application/` | Application layer |
| `backend/stockFlow/src/stockFlow.Infrastructure/` | Persistence and infrastructure |
| `backend/stockFlow/src/stockFlow.Api/` | API host |
| `backend/stockFlow/tests/` | Unit, architecture, and integration test projects |
| `frontend/stockFlow/` | Angular application, package scripts, and frontend documentation |
| `docs/` | Requirements, rules, data model, architecture, and planning |
| `scripts/` | Admin bootstrap and verification/release scripts |

> The Angular application currently lives in **`frontend/stockFlow`**, not directly in `frontend`. Use this path for local commands and Vercel's Root Directory.

## Getting Started

### Prerequisites

- A .NET 10 SDK compatible with the repository's [`global.json`](global.json). The current pin is `10.0.401` with patch roll-forward.
- SQL Server for application data and real SQL integration tests.
- Node.js **24.x**, or a supported **22.x** release starting at `22.12.0`.
- npm and Git. PowerShell is used by the setup examples below.

### 1. Clone the repository

```powershell
git clone https://github.com/Yusif-Alaidy/StockFlow.git
cd StockFlow
```

Run the backend commands below from the repository root.

### 2. Configure the local database connection

Store the connection in the API project's User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=StockFlowDb;Trusted_Connection=True;TrustServerCertificate=True" --project backend/stockFlow/src/stockFlow.Api/stockFlow.csproj
```

Adjust the SQL Server name for your installation. For example, a named SQL Express instance uses a different server name.

Do not commit real connection strings or database credentials to source control.

### 3. Restore, build, and apply migrations

```powershell
dotnet restore backend/stockFlow/stockFlow.slnx
dotnet build backend/stockFlow/stockFlow.slnx --no-restore
```

In Visual Studio's **Package Manager Console**:

```powershell
Update-Database -Project stockFlow.Infrastructure -StartupProject stockFlow -Context StockFlowDbContext
```

Alternatively, with EF Core CLI tools installed:

```powershell
dotnet ef database update --project backend/stockFlow/src/stockFlow.Infrastructure --startup-project backend/stockFlow/src/stockFlow.Api --context StockFlowDbContext
```

Use the existing migrations for a fresh database. The API does **not** automatically apply migrations on web startup.

### 4. Create the first administrator

After applying migrations, run the supplied bootstrap script from the repository root:

```powershell
.\scripts\bootstrap-admin.ps1 -Email "yourname@example.com"
```

The script prompts for a password without displaying it, then invokes the API's explicit `--bootstrap-admin` command. Passwords must meet the backend's requirements, including a minimum of 12 characters with a letter and a digit.

Bootstrap is available only **before the first user exists**. Additional accounts are created by an authenticated administrator. There is no public signup endpoint or default administrator password.

### 5. Start the API

```powershell
dotnet run --project backend/stockFlow/src/stockFlow.Api --launch-profile https
```

With the checked-in launch profile:

- API base: `https://localhost:7162/api/v1`
- Swagger UI: `https://localhost:7162/docs`
- OpenAPI document: `https://localhost:7162/openapi/v1.json`

Trust the local ASP.NET Core HTTPS development certificate if your machine has not already been configured for it.

### 6. Start the frontend

Edit [`frontend/stockFlow/public/app-config.json`](frontend/stockFlow/public/app-config.json) to select the API:

```json
{
  "apiBaseUrl": "https://localhost:7162/api/v1"
}
```

To use the hosted backend instead, set `apiBaseUrl` to `https://stock-flow.runasp.net/api/v1`.

Then open a separate terminal:

```powershell
cd frontend/stockFlow
npm ci
npm start
```

Open `http://localhost:4200` and sign in with the administrator account you created. Allow this origin in the API's CORS configuration.

## Testing

### Backend unit and architecture tests

From the repository root:

```powershell
dotnet test backend/stockFlow/tests/stockFlow.UnitTests/stockFlow.UnitTests.csproj
dotnet test backend/stockFlow/tests/stockFlow.ArchitectureTests/stockFlow.ArchitectureTests.csproj
```

### SQL integration tests

Provide a **local SQL Server** connection using an account allowed to create and drop test databases:

```powershell
$env:STOCKFLOW_TEST_SQL = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test backend/stockFlow/tests/stockFlow.IntegrationTests/stockFlow.IntegrationTests.csproj
Remove-Item Env:STOCKFLOW_TEST_SQL
```

SQL fixtures create isolated databases with generated `StockFlowTests_` names and remove them after use. They reject hosted database connections. Do not point test settings at production.

### Frontend checks

From `frontend/stockFlow`:

```powershell
npm test
npm run build
```

The frontend's recorded verification includes **40 passing tests** as of October 28, 2026. See the [verification report](frontend/stockFlow/docs/VERIFICATION.md) for the executed checks and limitations; this is recorded evidence, not a live CI status badge.

## API Conventions

- Business endpoints are versioned under `/api/v1`.
- Protected endpoints accept `Authorization: Bearer <access-token>`.
- Session tokens are opaque; clients do not decode them as JWTs.
- Refresh tokens rotate, and reuse detection can revoke the session family.
- Lists return `items`, `pageNumber`, `pageSize`, and `totalCount`.
- Versioned mutations require `If-Match: "<base64-version>"`.
- Errors use ProblemDetails with a stable application code and a trace ID.
- Health endpoints are unversioned: `/health/live` and `/health/ready`.
- Swagger/OpenAPI exposure is controlled by the environment and `Api:ExposeDocumentation`.

See the [frontend API map](frontend/stockFlow/docs/API-MAP.md) for route groups, request shapes, and client behavior.

## Deployment

### Frontend — Vercel

| Setting | Value |
| --- | --- |
| Framework Preset | Angular |
| Root Directory | `frontend/stockFlow` |
| Install Command | `npm ci` |
| Build Command | `npm run build` |
| Output Directory | `dist/stockflow/browser` |
| Node.js | `24.x` |

Keep [`vercel.json`](frontend/stockFlow/vercel.json) in the frontend project for Angular route fallback. The API URL is a public runtime setting in `app-config.json`; database credentials and application secrets do not belong in the frontend.

### Backend — MonsterASP

Configure the API host with its actual deployment values:

| Configuration key | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production`. |
| `ConnectionStrings__DefaultConnection` | SQL connection string for the hosted application. |
| `AllowedHosts` | The API hostname accepted by the application. |
| `Cors__AllowedOrigins__0` | An exact allowed frontend origin. |
| `Api__ExposeDocumentation` | Set explicitly if public API documentation should be exposed. |

For the current frontend, the CORS origin is `https://stock-flow-ten-dun.vercel.app` — no trailing slash and no page path. Additional development origins can use subsequent indexed keys. Restart the API after changing host configuration.

Apply migrations deliberately before use. Creating the first administrator is a separate bootstrap operation, not an automatic web startup task.

## Documentation

| Document | Purpose |
| --- | --- |
| [Requirements](docs/requirements.md) | MVP scope and acceptance requirements |
| [Business rules](docs/business-rules.md) | Inventory and purchasing invariants |
| [Data model](docs/data-model.md) | Entities and relationships |
| [Architecture](docs/architecture.md) | Backend project boundaries |
| [Backlog](docs/backlog.md) | Planned implementation work |
| [Progress](docs/progress.md) | Development progress notes |
| [Frontend guide](frontend/stockFlow/README-AR.md) | Arabic setup and usage instructions |
| [Frontend structure](frontend/stockFlow/docs/STRUCTURE-AR.md) | Client code organization |
| [Deployment guide](frontend/stockFlow/docs/DEPLOYMENT-AR.md) | Frontend hosting and configuration |
| [Manual acceptance tests](frontend/stockFlow/docs/MANUAL-TESTS.md) | Workflow checks before a release/demo |

Some planning documents describe earlier implementation phases. The source, API contract, and explicitly dated verification evidence should be consulted when checking current behavior.

## Scope & Future Improvements

The current MVP intentionally excludes partial receipts, returns, batch/serial tracking, expiry dates, multi-company tenancy, tax/accounting workflows, and currency conversion.

Potential next steps include reducing global write-lock contention, expanding automated browser coverage, and adding business features backed by explicit requirements and integration tests.

---

Built by [Yusif-Alaidy](https://github.com/Yusif-Alaidy).
