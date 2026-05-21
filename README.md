# SQL Visual Explorer

Desktop SQL analysis tool for query execution, execution-plan visualization, and optimization hints.

## Project Layout

- `src/SQLVisualExplorer.Desktop` - application entry point.
- `src/SQLVisualExplorer.UI` - views, controls, and view models.
- `src/SQLVisualExplorer.Application` - application services and use cases.
- `src/SQLVisualExplorer.Infrastructure` - database drivers, parsers, persistence, and external integrations.
- `src/SQLVisualExplorer.Domain` - domain models and enums.

## Current State

Initial solution skeleton with an Avalonia desktop bootstrap, shell UI, dependency injection wiring, EF Core SQLite model, startup database migration, and an initial SQLite migration. No database drivers, query execution, or plan-analysis implementation has been added yet.

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
