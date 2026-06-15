# Contributing

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- An Avalonia-compatible IDE (Rider, VS Code with C# Dev Kit) or any text editor

## Build

```bash
dotnet restore SQLVisualExplorer.sln
dotnet build SQLVisualExplorer.sln --no-restore /m:1
```

## Test

```bash
DOTNET_ROLL_FORWARD=Major dotnet test SQLVisualExplorer.sln --no-restore /m:1
```

## Run

```bash
dotnet run --project src/SQLVisualExplorer.Desktop/SQLVisualExplorer.Desktop.csproj
```

## EF Core migrations

Local EF tooling is pinned via `dotnet-tools.json`.

```bash
dotnet tool restore
DOTNET_ROLL_FORWARD=Major dotnet tool run dotnet-ef migrations add <Name> \
  --project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj \
  --startup-project src/SQLVisualExplorer.Infrastructure/SQLVisualExplorer.Infrastructure.csproj \
  --output-dir Database/Migrations
```

## Pull requests

- One logical change per PR.
- All existing tests must pass.
- New behaviour should include tests.
- Keep commits focused; squash fixups before opening review.

## License

By contributing you agree that your changes will be released under the [MIT License](LICENSE).
