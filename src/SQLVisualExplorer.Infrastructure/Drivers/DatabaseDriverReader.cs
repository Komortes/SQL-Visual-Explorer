using System.Data.Common;
using System.Diagnostics;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

internal static class DatabaseDriverReader
{
    public static async Task<QueryResult> ExecuteReaderAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await using (connection)
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;

            var rows = new List<IReadOnlyDictionary<string, object?>>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = ReadColumns(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(ReadRow(reader));
            }

            stopwatch.Stop();
            var recordsAffected = reader.RecordsAffected;

            return new QueryResult
            {
                Duration = stopwatch.Elapsed,
                RowCount = rows.Count > 0 || recordsAffected < 0 ? rows.Count : recordsAffected,
                Columns = columns,
                Rows = rows
            };
        }
    }

    public static async Task<string> ExecuteScalarStringAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using (connection)
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value?.ToString() ?? string.Empty;
        }
    }

    public static async Task<string> ExecuteSingleColumnTextAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using (connection)
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;

            var lines = new List<string>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                lines.Add(reader.IsDBNull(0) ? string.Empty : reader.GetValue(0).ToString() ?? string.Empty);
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    private static IReadOnlyList<string> ReadColumns(DbDataReader reader)
    {
        var columns = new List<string>();

        for (var index = 0; index < reader.FieldCount; index++)
        {
            columns.Add(reader.GetName(index));
        }

        return columns;
    }

    private static IReadOnlyDictionary<string, object?> ReadRow(DbDataReader reader)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < reader.FieldCount; index++)
        {
            var value = reader.IsDBNull(index) ? null : reader.GetValue(index);
            row[reader.GetName(index)] = value;
        }

        return row;
    }
}
