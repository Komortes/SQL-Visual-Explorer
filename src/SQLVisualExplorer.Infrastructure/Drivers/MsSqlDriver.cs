using Microsoft.Data.SqlClient;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public sealed class MsSqlDriver : IDatabaseDriver
{
    public bool Supports(DatabaseType databaseType) => databaseType == DatabaseType.SqlServer;

    public async Task<ConnectionTestResult> TestConnectionAsync(
        Connection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbConnection = new SqlConnection(CreateConnectionString(connection));
            await dbConnection.OpenAsync(cancellationToken);
            return ConnectionTestResult.Success("SQL Server connection succeeded.");
        }
        catch (Exception ex) when (ex is SqlException or TimeoutException or InvalidOperationException)
        {
            return ConnectionTestResult.Failure(ex.Message);
        }
    }

    public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        var dbConnection = new SqlConnection(CreateConnectionString(connection));
        return DatabaseDriverReader.ExecuteReaderAsync(dbConnection, sql, cancellationToken);
    }

    public async Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(CreateConnectionString(connection));
        await dbConnection.OpenAsync(cancellationToken);
        return await FetchShowplanXmlAsync(dbConnection, NormalizeSql(sql), "SHOWPLAN_XML", cancellationToken);
    }

    public async Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(CreateConnectionString(connection));
        await dbConnection.OpenAsync(cancellationToken);
        return await FetchShowplanXmlAsync(dbConnection, NormalizeSql(sql), "STATISTICS XML", cancellationToken);
    }

    private static async Task<string> FetchShowplanXmlAsync(
        SqlConnection connection,
        string sql,
        string setting,
        CancellationToken cancellationToken)
    {
        await using (var onCmd = connection.CreateCommand())
        {
            onCmd.CommandText = $"SET {setting} ON";
            await onCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        string xmlPlan;

        if (setting == "SHOWPLAN_XML")
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 30;
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            xmlPlan = result?.ToString() ?? string.Empty;
        }
        else
        {
            // STATISTICS XML: the plan is returned as an extra result set after the data
            xmlPlan = string.Empty;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 30;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            do
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (reader.FieldCount == 1 && !reader.IsDBNull(0))
                    {
                        var val = reader.GetValue(0)?.ToString() ?? string.Empty;
                        if (val.StartsWith("<ShowPlanXML", StringComparison.Ordinal))
                            xmlPlan = val;
                    }
                }
            }
            while (await reader.NextResultAsync(cancellationToken));
        }

        await using (var offCmd = connection.CreateCommand())
        {
            offCmd.CommandText = $"SET {setting} OFF";
            await offCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        return xmlPlan;
    }

    private static string NormalizeSql(string sql) => sql.Trim().TrimEnd(';');

    private static string CreateConnectionString(Connection connection)
    {
        var host = connection.Host ?? "localhost";
        var port = connection.Port ?? 1433;
        var builder = new SqlConnectionStringBuilder
        {
            DataSource          = $"{host},{port}",
            InitialCatalog      = connection.Database ?? string.Empty,
            ConnectTimeout         = 5,
            Encrypt                = SqlConnectionEncryptOption.Mandatory,
            // UseSsl=false means dev/self-signed cert is acceptable; UseSsl=true enforces full verification.
            TrustServerCertificate = !connection.UseSsl,
            Pooling                = false,
        };

        if (string.IsNullOrWhiteSpace(connection.Username))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID   = connection.Username;
            builder.Password = connection.Password ?? string.Empty;
        }

        return builder.ConnectionString;
    }
}
