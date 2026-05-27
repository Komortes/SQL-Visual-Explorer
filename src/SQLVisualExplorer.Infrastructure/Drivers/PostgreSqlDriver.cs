using Npgsql;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public sealed class PostgreSqlDriver : IDatabaseDriver
{
    public bool Supports(DatabaseType databaseType)
    {
        return databaseType == DatabaseType.PostgreSql;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        Connection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbConnection = new NpgsqlConnection(CreateConnectionString(connection));
            await dbConnection.OpenAsync(cancellationToken);

            return ConnectionTestResult.Success("PostgreSQL connection succeeded.");
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return ConnectionTestResult.Failure(exception.Message);
        }
    }

    public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new NpgsqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteReaderAsync(dbConnection, sql, cancellationToken);
    }

    public Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new NpgsqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteScalarStringAsync(
            dbConnection,
            $"EXPLAIN (FORMAT JSON) {NormalizeSql(sql)}",
            cancellationToken);
    }

    public Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new NpgsqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteScalarStringAsync(
            dbConnection,
            $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {NormalizeSql(sql)}",
            cancellationToken);
    }

    private static string NormalizeSql(string sql)
    {
        return sql.Trim().TrimEnd(';');
    }

    private static string CreateConnectionString(Connection connection)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host ?? "localhost",
            Port = connection.Port ?? 5432,
            Database = connection.Database,
            Username = connection.Username,
            Password = connection.Password,
            Timeout = 5,
            CommandTimeout = 5,
            Pooling = false
        };

        if (connection.UseSsl)
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }
}
