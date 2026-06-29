# SQL Visual Explorer

Desktop SQL analysis tool for query execution, execution-plan visualization, and optimization hints.

![Editor view](docs/screenshots/editor.png)
![Plan graph view](docs/screenshots/plan-graph.png)
![Compare view](docs/screenshots/compare.png)

## Features

- **Multi-database support** — PostgreSQL, MySQL, MariaDB, SQLite, and SQL Server
- **Query execution** — run queries with a resizable results grid, column sorting, and CSV/JSON export
- **Execution plan visualization** — tree, node graph, and flame graph views from `EXPLAIN ANALYZE`
- **Plan analysis** — automatic detection of seq scans, nested-loop issues, row-estimate mismatches, missing indexes, and more
- **Plan comparison** — run two queries side-by-side and see cost/time/node-count diff with a winner indicator
- **Snippet management** — save, tag, and search reusable SQL snippets
- **Query history** — every run is recorded locally; reopen SQL or reload saved plans
- **AI Advisor** — optional OpenAI-compatible endpoint that suggests rewrites and optimizations
- **Secure credential storage** — passwords stored in OS keychain (Keychain on macOS, DPAPI on Windows, Secret Service on Linux)

## Project Layout

```
src/
  SQLVisualExplorer.Desktop        # application entry point (Avalonia)
  SQLVisualExplorer.UI             # views, controls, and view models
  SQLVisualExplorer.Application    # service interfaces and use cases
  SQLVisualExplorer.Infrastructure # database drivers, parsers, persistence
  SQLVisualExplorer.Domain         # domain models and enums
tests/
  SQLVisualExplorer.Infrastructure.Tests
```

## Build

Target framework: `.NET 10` (`net10.0`).

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

## EF Core migrations

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
