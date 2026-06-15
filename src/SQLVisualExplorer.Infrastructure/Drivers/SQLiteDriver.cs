using Microsoft.Data.Sqlite;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public sealed class SQLiteDriver : IDatabaseDriver
{
    public bool Supports(DatabaseType databaseType) => databaseType == DatabaseType.SQLite;

    public async Task<ConnectionTestResult> TestConnectionAsync(
        Connection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = new SqliteConnection(CreateConnectionString(connection));
            await db.OpenAsync(cancellationToken);
            return ConnectionTestResult.Success("SQLite connection succeeded.");
        }
        catch (Exception ex) when (ex is SqliteException or InvalidOperationException or IOException)
        {
            return ConnectionTestResult.Failure(ex.Message);
        }
    }

    public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var db = new SqliteConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteReaderAsync(db, sql, cancellationToken);
    }

    public Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        return ReadExplainQueryPlanAsync(connection, sql, cancellationToken);
    }

    public Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        return ReadExplainQueryPlanAsync(connection, sql, cancellationToken);
    }

    private static async Task<string> ReadExplainQueryPlanAsync(
        Connection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var db = new SqliteConnection(CreateConnectionString(connection));
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        command.CommandText = $"EXPLAIN QUERY PLAN {NormalizeSql(sql)}";
        command.CommandTimeout = 30;

        var lines = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id     = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var parent = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var detail = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            lines.Add($"{id}|{parent}|{detail}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string NormalizeSql(string sql) => sql.Trim().TrimEnd(';');

    private static string CreateConnectionString(Connection connection)
    {
        var dataSource = connection.Database ?? ":memory:";
        return new SqliteConnectionStringBuilder
        {
            DataSource = dataSource,
            Mode       = dataSource == ":memory:" ? SqliteOpenMode.Memory : SqliteOpenMode.ReadWriteCreate,
        }.ConnectionString;
    }
}
