namespace SQLVisualExplorer.Application.Services;

public static class SqlFormatter
{
    private static readonly string[] ClauseKeywords =
    [
        "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "HAVING",
        "LIMIT", "OFFSET", "UNION ALL", "UNION", "INTERSECT", "EXCEPT",
        "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE FROM",
        "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "FULL OUTER JOIN",
        "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "CROSS JOIN", "JOIN",
        "ON", "WITH",
    ];

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT","FROM","WHERE","AND","OR","NOT","IN","IS","NULL","AS","ON",
        "JOIN","LEFT","RIGHT","INNER","OUTER","FULL","CROSS","NATURAL",
        "GROUP","ORDER","BY","HAVING","LIMIT","OFFSET","UNION","INTERSECT",
        "EXCEPT","ALL","DISTINCT","INTO","VALUES","UPDATE","SET","DELETE",
        "INSERT","WITH","CASE","WHEN","THEN","ELSE","END","EXISTS",
        "BETWEEN","LIKE","ILIKE","SIMILAR","ESCAPE","TRUE","FALSE",
        "ASC","DESC","NULLS","FIRST","LAST","OVER","PARTITION","ROWS",
        "RANGE","UNBOUNDED","PRECEDING","FOLLOWING","CURRENT","ROW",
        "RECURSIVE","LATERAL","TABLESAMPLE","ONLY","RETURNING","CONFLICT",
        "DO","NOTHING","EXCLUDED","USING","FILTER","WITHIN",
        "CREATE","TABLE","INDEX","VIEW","ALTER","DROP","TRUNCATE",
        "ANALYZE","EXPLAIN","VACUUM","PRIMARY","KEY","UNIQUE","FOREIGN",
        "REFERENCES","CHECK","DEFAULT","CONSTRAINT",
    };

    public static string Format(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        var tokens = Tokenize(sql);
        var upper  = UppercaseKeywords(tokens);
        return BuildFormatted(upper);
    }

    private enum TokenKind { Word, Whitespace, StringLiteral, LineComment, BlockComment, Punctuation }
    private sealed record Token(TokenKind Kind, string Text);

    private static List<Token> Tokenize(string sql)
    {
        var result = new List<Token>();
        var i = 0;
        while (i < sql.Length)
        {
            var ch = sql[i];

            if (ch == '\'')
            {
                var start = i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '\'' && (i + 1 >= sql.Length || sql[i + 1] != '\'')) { i++; break; }
                    if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'') { i += 2; continue; }
                    i++;
                }
                result.Add(new Token(TokenKind.StringLiteral, sql[start..i]));
                continue;
            }

            if (ch == '"')
            {
                var start = i++;
                while (i < sql.Length && sql[i] != '"') i++;
                if (i < sql.Length) i++;
                result.Add(new Token(TokenKind.StringLiteral, sql[start..i]));
                continue;
            }

            if (ch == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                var start = i;
                while (i < sql.Length && sql[i] != '\n') i++;
                result.Add(new Token(TokenKind.LineComment, sql[start..i]));
                continue;
            }

            if (ch == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                var start = i; i += 2;
                while (i < sql.Length && !(sql[i - 1] == '*' && sql[i] == '/')) i++;
                if (i < sql.Length) i++;
                result.Add(new Token(TokenKind.BlockComment, sql[start..i]));
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                var start = i;
                while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
                result.Add(new Token(TokenKind.Whitespace, sql[start..i]));
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_')) i++;
                result.Add(new Token(TokenKind.Word, sql[start..i]));
                continue;
            }

            result.Add(new Token(TokenKind.Punctuation, ch.ToString()));
            i++;
        }
        return result;
    }

    private static List<Token> UppercaseKeywords(List<Token> tokens) =>
        tokens.Select(t =>
            t.Kind == TokenKind.Word && Keywords.Contains(t.Text)
                ? t with { Text = t.Text.ToUpperInvariant() }
                : t
        ).ToList();

    private static string BuildFormatted(List<Token> tokens)
    {
        var flat = string.Concat(tokens.Select(t =>
            t.Kind == TokenKind.Whitespace ? " " : t.Text));

        var sorted = ClauseKeywords.OrderByDescending(k => k.Length).ToArray();
        var result = new System.Text.StringBuilder();
        var pos = 0;
        var parenDepth = 0;
        var inString = false;

        while (pos < flat.Length)
        {
            var ch = flat[pos];

            if (!inString && ch == '\'') { inString = true;  result.Append(ch); pos++; continue; }
            if (inString  && ch == '\'') { inString = false; result.Append(ch); pos++; continue; }
            if (inString)                { result.Append(ch); pos++; continue; }

            if (ch == '(') { parenDepth++; result.Append(ch); pos++; continue; }
            if (ch == ')') { parenDepth--; result.Append(ch); pos++; continue; }

            if (parenDepth == 0)
            {
                var matched = false;
                foreach (var kw in sorted)
                {
                    if (pos + kw.Length > flat.Length) continue;
                    var candidate = flat.Substring(pos, kw.Length);
                    var afterPos  = pos + kw.Length;
                    var before    = pos == 0 || !char.IsLetterOrDigit(flat[pos - 1]);
                    var after     = afterPos >= flat.Length || !char.IsLetterOrDigit(flat[afterPos]);
                    if (!string.Equals(candidate, kw, StringComparison.OrdinalIgnoreCase) || !before || !after)
                        continue;

                    var current = result.ToString().TrimEnd();
                    if (current.Length > 0)
                        result.Clear().Append(current).Append('\n');
                    result.Append(kw);
                    pos += kw.Length;
                    while (pos < flat.Length && flat[pos] == ' ') pos++;
                    if (pos < flat.Length && flat[pos] != '\n')
                        result.Append(' ');
                    matched = true;
                    break;
                }
                if (!matched) { result.Append(ch); pos++; }
                continue;
            }

            result.Append(ch);
            pos++;
        }

        var lines = result.ToString().Split('\n');
        var sb2   = new System.Text.StringBuilder();
        var inWhere = false;
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                inWhere = true;
            else if (ClauseKeywords.Any(k =>
                trimmed.StartsWith(k, StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase)))
                inWhere = false;

            if (inWhere && (trimmed.StartsWith("AND ", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("OR ",  StringComparison.OrdinalIgnoreCase)))
                sb2.AppendLine("  " + trimmed);
            else
                sb2.AppendLine(line);
        }

        return sb2.ToString().Trim();
    }
}
