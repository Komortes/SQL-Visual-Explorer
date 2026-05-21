using Microsoft.EntityFrameworkCore;

namespace SQLVisualExplorer.Infrastructure.Database;

public sealed class LocalDatabaseInitializer(AppDbContext dbContext) : ILocalDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}
