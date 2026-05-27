using MySqlConnector;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public sealed class MySqlDriver : IDatabaseDriver
{
    public bool Supports(DatabaseType databaseType)
    {
        return databaseType is DatabaseType.MySql or DatabaseType.MariaDb;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        Connection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbConnection = new MySqlConnection(CreateConnectionString(connection));
            await dbConnection.OpenAsync(cancellationToken);

            return ConnectionTestResult.Success("MySQL/MariaDB connection succeeded.");
        }
        catch (Exception exception) when (exception is MySqlException or TimeoutException or InvalidOperationException)
        {
            return ConnectionTestResult.Failure(exception.Message);
        }
    }

    public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new MySqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteReaderAsync(dbConnection, sql, cancellationToken);
    }

    public Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new MySqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteScalarStringAsync(
            dbConnection,
            $"EXPLAIN FORMAT=JSON {NormalizeSql(sql)}",
            cancellationToken);
    }

    public Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new MySqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteSingleColumnTextAsync(
            dbConnection,
            $"EXPLAIN ANALYZE {NormalizeSql(sql)}",
            cancellationToken);
    }

    private static string NormalizeSql(string sql)
    {
        return sql.Trim().TrimEnd(';');
    }

    private static string CreateConnectionString(Connection connection)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = connection.Host ?? "localhost",
            Port = (uint)(connection.Port ?? 3306),
            Database = connection.Database,
            UserID = connection.Username,
            Password = connection.Password,
            ConnectionTimeout = 5,
            DefaultCommandTimeout = 5,
            Pooling = false,
            SslMode = connection.UseSsl ? MySqlSslMode.Required : MySqlSslMode.Preferred
        };

        return builder.ConnectionString;
    }
}
