# StockFlow Architecture Foundation

## Status

Phase 01 establishes project boundaries and composition only. No business entities, workflows, persistence mappings, authentication, endpoints, or feature handlers are implemented. The target framework remains `net10.0`, matching the existing API project and installed SDK `10.0.401`; restore/build/test verification is recorded in `docs/progress.md` and does not by itself confirm production hosting compatibility.

## Repository layout

```text
StockFlow/
├── backend/stockFlow/
│   ├── stockFlow.csproj                         API host (existing project)
│   ├── Program.cs                               composition root
│   ├── src/
│   │   ├── stockFlow.Domain/                   business model and rules
│   │   ├── stockFlow.Application/               use cases and feature organization
│   │   └── stockFlow.Infrastructure/            persistence and external implementations
│   └── tests/
│       ├── stockFlow.UnitTests/
│       ├── stockFlow.IntegrationTests/
│       └── stockFlow.ArchitectureTests/
├── docs/
├── frontend/
├── global.json
└── .editorconfig
```

This remains one application with one planned database. Microservices and additional infrastructure are out of scope.

## Responsibilities

### Domain

Contains business concepts, invariants, value objects, and domain services when required. It must remain independent of EF Core, ASP.NET Core, Identity, Infrastructure, Application, and dependency-injection packages. The project is intentionally empty in Phase 01.

### Application

Contains feature-based use cases, ports/abstractions, validation orchestration, and application-level authorization decisions. It may reference Domain, but must not reference Infrastructure or the API host. Feature folders will be added with the corresponding use cases; Phase 01 does not generate placeholder handlers or DTOs.

### Infrastructure

Contains SQL Server, EF Core, Identity, external-system, and other technical implementations when later phases require them. It references Application and Domain. Its `AddInfrastructure` entry point currently registers no services.

### API

The existing `stockFlow` project is the ASP.NET Core host. It owns HTTP composition, controllers, OpenAPI, middleware, and transport concerns. It references Application and Infrastructure for composition. Its `Program.cs` calls both minimal DI entry points; no business endpoints were added in Phase 01.

### Tests

- **UnitTests**: isolated Domain/Application tests when business code exists.
- **IntegrationTests**: application/API integration tests against explicitly configured test infrastructure; no database or fake test was added in Phase 01.
- **ArchitectureTests**: project dependency checks. These tests parse actual `.csproj` files to verify project and package references; assembly/type checks alone are deliberately not treated as sufficient for empty projects.

## Allowed dependency directions

```text
Domain                 -> none
Application            -> Domain
Infrastructure         -> Application, Domain
API                    -> Application, Infrastructure
UnitTests              -> Application, Domain (as needed)
IntegrationTests       -> API (and later approved test infrastructure)
ArchitectureTests      -> projects under inspection for test visibility
```

The test projects are verification consumers and are not part of the production dependency graph. Application must not reference Infrastructure or API. Domain must not reference any application layer, EF Core, Identity, ASP.NET Core, or Infrastructure.

## Composition

`backend/stockFlow/Program.cs` is the composition root:

1. Add MVC controllers and the existing OpenAPI service.
2. Call `AddApplication()` from `stockFlow.Application`.
3. Call `AddInfrastructure()` from `stockFlow.Infrastructure`.
4. Build and run the existing API pipeline.

The entry points are intentionally empty until later phases introduce approved services. No DI package is referenced by Domain.

## Framework and package policy

- `global.json` pins the installed stable SDK `10.0.401` with `latestPatch` roll-forward.
- All backend projects inherit `net10.0`, nullable reference types, and implicit usings from `backend/stockFlow/Directory.Build.props`.
- The existing API OpenAPI package is preserved at `10.0.12`.
- Application and Infrastructure use only `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.1` for the required DI entry points.
- Test projects use `Microsoft.NET.Test.Sdk`, xUnit, and the Visual Studio xUnit adapter only for test infrastructure.
- EF Core, Identity, MediatR, SQL Server providers, and other future-feature packages are intentionally not added in Phase 01.

## Verification boundaries and pending decisions

Architecture tests verify project-file reference direction and forbidden Domain packages. They do not prove runtime hosting, database behavior, authorization, transaction isolation, or business correctness. Those decisions remain governed by the Phase 00 documents and must be confirmed before Phase 02. Hosting compatibility is pending actual restore/build/test results and the deployment environment.
