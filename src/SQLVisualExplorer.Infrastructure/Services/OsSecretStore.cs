using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SQLVisualExplorer.Application.Services;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class OsSecretStore : ISecretStore
{
    private const string AppName = "sql-visual-explorer";

    public Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            SaveWindows(key, secret);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            SaveMacOs(key, secret);
        else
            SaveLinux(key, secret);

        return Task.CompletedTask;
    }

    public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        string? result = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            result = LoadWindows(key);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            result = LoadMacOs(key);
        else
            result = LoadLinux(key);

        return Task.FromResult(result);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            DeleteWindows(key);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            DeleteMacOs(key);
        else
            DeleteLinux(key);

        return Task.CompletedTask;
    }

    // ── Windows: DPAPI ───────────────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    private static void SaveWindows(string key, string secret)
    {
        var plain  = Encoding.UTF8.GetBytes(secret);
        var cipher = System.Security.Cryptography.ProtectedData.Protect(
            plain, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        File.WriteAllBytes(WindowsPath(key), cipher);
    }

    [SupportedOSPlatform("windows")]
    private static string? LoadWindows(string key)
    {
        var path = WindowsPath(key);
        if (!File.Exists(path)) return null;
        var cipher = File.ReadAllBytes(path);
        var plain  = System.Security.Cryptography.ProtectedData.Unprotect(
            cipher, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    [SupportedOSPlatform("windows")]
    private static void DeleteWindows(string key)
    {
        var path = WindowsPath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    [SupportedOSPlatform("windows")]
    private static string WindowsPath(string key)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName, "secrets");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, SanitizeKey(key) + ".dat");
    }

    // ── macOS: security CLI ──────────────────────────────────────────────────

    [SupportedOSPlatform("macos")]
    private static void SaveMacOs(string key, string secret)
    {
        RunSecurity("delete-generic-password", "-s", AppName, "-a", SanitizeKey(key));
        RunSecurity("add-generic-password", "-s", AppName, "-a", SanitizeKey(key), "-w", secret);
    }

    [SupportedOSPlatform("macos")]
    private static string? LoadMacOs(string key)
    {
        var output = RunSecurity("find-generic-password", "-s", AppName, "-a", SanitizeKey(key), "-w");
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    [SupportedOSPlatform("macos")]
    private static void DeleteMacOs(string key)
    {
        RunSecurity("delete-generic-password", "-s", AppName, "-a", SanitizeKey(key));
    }

    private static string? RunSecurity(params string[] argv)
    {
        try
        {
            var psi = new ProcessStartInfo("security")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            foreach (var arg in argv)
                psi.ArgumentList.Add(arg);

            using var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return proc.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    // ── Linux: Secret Service ───────────────────────────────────────────────

    [UnsupportedOSPlatform("windows")]
    private static void SaveLinux(string key, string secret)
    {
        var safeKey = SanitizeKey(key);
        if (TrySecretTool(secret, "store", $"--label={AppName}/{safeKey}", "app", AppName, "key", safeKey))
            return;

        throw new InvalidOperationException(
            "Linux Secret Service is unavailable. Install and unlock GNOME Keyring, KWallet, or another Secret Service provider before saving passwords.");
    }

    [UnsupportedOSPlatform("windows")]
    private static string? LoadLinux(string key)
    {
        var safeKey = SanitizeKey(key);
        var result  = RunSecretTool("lookup", "app", AppName, "key", safeKey);
        return result?.Trim();
    }

    [UnsupportedOSPlatform("windows")]
    private static void DeleteLinux(string key)
    {
        var safeKey = SanitizeKey(key);
        RunSecretTool("clear", "app", AppName, "key", safeKey);
    }

    private static bool TrySecretTool(string secret, params string[] argv)
    {
        try
        {
            var psi = new ProcessStartInfo("secret-tool")
            {
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            foreach (var arg in argv)
                psi.ArgumentList.Add(arg);

            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.StandardInput.WriteLine(secret);
            proc.StandardInput.Close();
            proc.WaitForExit();
            return proc.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string? RunSecretTool(params string[] argv)
    {
        try
        {
            var psi = new ProcessStartInfo("secret-tool")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            foreach (var arg in argv)
                psi.ArgumentList.Add(arg);

            using var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return proc.ExitCode == 0 ? output : null;
        }
        catch { return null; }
    }

    private static string SanitizeKey(string key) =>
        string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));
}
