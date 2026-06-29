using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class OpenAiAdvisorService : IQueryAdvisorService
{
    private readonly ISecretStore _secretStore;
    private readonly HttpClient _http = new();
    private volatile bool _isConfigured;

    public OpenAiAdvisorService(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    public bool IsConfigured => _isConfigured;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var key = await _secretStore.LoadAsync("advisor-api-key", cancellationToken);
        _isConfigured = !string.IsNullOrWhiteSpace(key);
    }

    public async Task<AdvisorResult> AnalyzeAsync(
        string sql,
        string databaseType,
        IEnumerable<PlanIssue> issues,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _secretStore.LoadAsync("advisor-api-key", cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("AI Advisor API key is not configured.");

        var endpoint = await _secretStore.LoadAsync("advisor-endpoint", cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "https://api.openai.com/v1";

        var model = await _secretStore.LoadAsync("advisor-model", cancellationToken);
        if (string.IsNullOrWhiteSpace(model)) model = "gpt-4o-mini";

        var issueList = issues.ToList();
        var issuesText = issueList.Count == 0
            ? "None detected."
            : string.Join("\n", issueList.Select(i => $"- [{i.Severity}] {i.Code}: {i.Description}"));

        const string systemPrompt =
            "You are a SQL performance expert. Analyze the query and issues. " +
            "Respond ONLY in JSON with fields: summary (string), suggestions (array of strings), " +
            "rewritten_sql (string, empty string if no rewrite needed). No markdown fences, raw JSON only.";

        var userPrompt = $"Database: {databaseType}\n\nSQL:\n{sql}\n\nDetected Issues:\n{issuesText}\n\nProvide optimization advice.";

        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            },
            temperature = 0.2,
            max_tokens  = 1000
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{endpoint.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return ParseAdvisorResult(content);
    }

    private static AdvisorResult ParseAdvisorResult(string content)
    {
        try
        {
            var json = content.Trim();
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var lines = json.Split('\n');
                json = string.Join("\n", lines.Skip(1).SkipLast(1)).Trim();
            }

            using var resultDoc = JsonDocument.Parse(json);
            var root = resultDoc.RootElement;

            var summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";

            List<string> suggestions = [];
            if (root.TryGetProperty("suggestions", out var sg) && sg.ValueKind == JsonValueKind.Array)
                suggestions = sg.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(x => x.Length > 0)
                    .ToList();

            var rewrittenSql = root.TryGetProperty("rewritten_sql", out var rs) ? rs.GetString() ?? "" : "";

            return new AdvisorResult { Summary = summary, Suggestions = suggestions, RewrittenSql = rewrittenSql };
        }
        catch
        {
            return new AdvisorResult { Summary = content };
        }
    }
}
