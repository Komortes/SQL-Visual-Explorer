using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Drivers;
using SQLVisualExplorer.Infrastructure.Parsers;
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
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IQueryExecutionService, QueryExecutionService>();
        services.AddScoped<IExplainAnalyzeService, ExplainAnalyzeService>();
        services.AddScoped<IPlanParserService, PlanParserService>();
        services.AddScoped<IPlanAnalyzerService, PlanAnalyzerService>();
        services.AddSingleton<IExplainParser, PostgreSqlExplainParser>();
        services.AddSingleton<IExplainParser, MySqlExplainParser>();
        services.AddSingleton<IExplainParser, SQLiteExplainParser>();
        services.AddSingleton<IDatabaseDriver, PostgreSqlDriver>();
        services.AddSingleton<IDatabaseDriver, MySqlDriver>();
        services.AddSingleton<IDatabaseDriver, SQLiteDriver>();

        return services;
    }
}
