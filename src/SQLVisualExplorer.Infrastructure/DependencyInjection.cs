using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(LocalDatabase.CreateDefaultConnectionString());
        });

        services.AddScoped<ILocalDatabaseInitializer, LocalDatabaseInitializer>();
        services.AddScoped<IConnectionService, ConnectionService>();

        return services;
    }
}
