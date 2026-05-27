using System.Text;

namespace SQLVisualExplorer.Infrastructure.Services;

internal static class SqlSafetyGuard
{
    private static readonly string[] ReadOnlyPrefixes =
    [
        "select",
        "with",
        "show",
        "describe",
        "desc",
        "explain"
    ];

    private static readonly string[] MutatingPrefixes =
    [
        "insert",
        "update",
        "delete",
        "merge",
        "replace",
        "create",
        "alter",
        "drop",
        "truncate",
        "rename",
        "grant",
        "revoke",
        "call",
        "do",
        "set"
    ];

    public static void ThrowIfNotSafeForExplainAnalyze(string sql)
    {
        var normalized = RemoveLeadingComments(sql).Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("SQL text is required.", nameof(sql));
        }

        if (HasStatementSeparator(normalized))
        {
            throw new InvalidOperationException("Analyze supports a single read-only SQL statement only.");
        }

        var firstToken = ReadFirstToken(normalized);

        if (MutatingPrefixes.Contains(firstToken, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Analyze can run only read-only SQL. Mutating statements are blocked.");
        }

        if (!ReadOnlyPrefixes.Contains(firstToken, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Analyze can run only read-only SQL statements.");
        }
    }

    private static string RemoveLeadingComments(string sql)
    {
        var current = sql.AsSpan().TrimStart();

        while (current.Length > 0)
        {
            if (current.StartsWith("--", StringComparison.Ordinal))
            {
                var newlineIndex = current.IndexOf('\n');
                current = newlineIndex < 0 ? [] : current[(newlineIndex + 1)..].TrimStart();
                continue;
            }

            if (current.StartsWith("/*", StringComparison.Ordinal))
            {
                var endIndex = current.IndexOf("*/", StringComparison.Ordinal);

                if (endIndex < 0)
                {
                    return string.Empty;
                }

                current = current[(endIndex + 2)..].TrimStart();
                continue;
            }

            break;
        }

        return current.ToString();
    }

    private static bool HasStatementSeparator(string sql)
    {
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var index = 0; index < sql.Length; index++)
        {
            var current = sql[index];
            var next = index + 1 < sql.Length ? sql[index + 1] : '\0';

            if (inLineComment)
            {
                if (current == '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    inBlockComment = false;
                    index++;
                }

                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && current == '-' && next == '-')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && current == '/' && next == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (!inDoubleQuote && current == '\'' && !IsEscaped(sql, index))
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (!inSingleQuote && current == '"' && !IsEscaped(sql, index))
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && current == ';' && !IsOnlyTrailingSemicolon(sql, index))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOnlyTrailingSemicolon(string sql, int semicolonIndex)
    {
        return string.IsNullOrWhiteSpace(sql[(semicolonIndex + 1)..]);
    }

    private static bool IsEscaped(string sql, int index)
    {
        var slashCount = 0;

        for (var candidate = index - 1; candidate >= 0 && sql[candidate] == '\\'; candidate--)
        {
            slashCount++;
        }

        return slashCount % 2 == 1;
    }

    private static string ReadFirstToken(string sql)
    {
        var builder = new StringBuilder();

        foreach (var character in sql)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
