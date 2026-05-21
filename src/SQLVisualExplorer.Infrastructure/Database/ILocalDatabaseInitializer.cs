namespace SQLVisualExplorer.Infrastructure.Database;

public interface ILocalDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
