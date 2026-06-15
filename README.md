# SQL Visual Explorer

Desktop SQL analysis tool for query execution, execution-plan visualization, and optimization hints.

## Project Layout

- `src/SQLVisualExplorer.Desktop` - application entry point.
- `src/SQLVisualExplorer.UI` - views, controls, and view models.
- `src/SQLVisualExplorer.Application` - application services and use cases.
- `src/SQLVisualExplorer.Infrastructure` - database drivers, parsers, persistence, and external integrations.
- `src/SQLVisualExplorer.Domain` - domain models and enums.

## Current State

Avalonia desktop application with dependency injection, EF Core SQLite persistence, and startup migrations. Supports four database engines: **PostgreSQL, MySQL, MariaDB, and SQLite** — each with connection testing and full query execution. Query results are shown in a resizable grid; runs are recorded in local history and can be reopened in the editor.

Execution plans are fetched via `EXPLAIN ANALYZE` (PostgreSQL/MySQL/MariaDB) and `EXPLAIN QUERY PLAN` (SQLite), parsed, and rendered as an interactive node graph (`PlanGraphControl`) with a graph layout engine that positions nodes by dependency depth. Each node shows cost, actual vs. estimated row counts, and detected issues (seq scan, high cost, row estimate mismatch, etc.).

**Plan comparison** lets you run two queries side by side and see a summary diff — cost, timing, node count, and a winner indicator.

**Snippet management** allows saving, browsing, and reusing SQL snippets across sessions.

Passwords are accepted for connection testing and session query execution but are not persisted.

## Build

Target framework: `.NET 9` (`net9.0`).

```bash
dotnet restore SQLVisualExplorer.sln
dotnet build SQLVisualExplorer.sln --no-restore /m:1
```

`/m:1` keeps solution builds deterministic in the current local environment.

## Test

```bash
DOTNET_ROLL_FORWARD=Major dotnet test SQLVisualExplorer.sln --no-restore /m:1
```

## Run

```bash
dotnet run --project src/SQLVisualExplorer.Desktop/SQLVisualExplorer.Desktop.csproj
```

## EF Core

Local EF tooling is pinned through `dotnet-tools.json`.

```bash
dotnet tool restore
DOTNET_ROLL_FORWARD=Major dotnet tool run dotnet-ef migrations add <MigrationName> \
  --project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj \
  --startup-project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj \
  --output-dir Database/Migrations

DOTNET_ROLL_FORWARD=Major dotnet tool run dotnet-ef database update \
  --project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj \
  --startup-project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj
```
