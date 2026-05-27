namespace SQLVisualExplorer.Application.Services;

public sealed class ConnectionTestResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public static ConnectionTestResult Success(string message)
    {
        return new ConnectionTestResult
        {
            Succeeded = true,
            Message = message
        };
    }

    public static ConnectionTestResult Failure(string message)
    {
        return new ConnectionTestResult
        {
            Succeeded = false,
            Message = message
        };
    }
}
