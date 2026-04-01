using System.Diagnostics;

namespace CryptoFear.Services;

public static class AppLog
{
    private const string DebugLogPrefKey = "debug_logging_enabled";
    private static string? _logFilePath;

    public static bool IsEnabled
    {
        get => Preferences.Get(DebugLogPrefKey, false);
        set => Preferences.Set(DebugLogPrefKey, value);
    }

    private static string LogFilePath =>
        _logFilePath ??= Path.Combine(FileSystem.AppDataDirectory, "debug.log");

    public static void Info(string message)
    {
        if (!IsEnabled) return;
        var line = FormatLine("INFO ", message);
        Debug.WriteLine(line);
        WriteToFile(line);
    }

    public static void Error(string message)
    {
        var line = FormatLine("ERROR", message);
        Debug.WriteLine(line);
        if (IsEnabled)
            WriteToFile(line);
    }

    private static string FormatLine(string level, string message) =>
        $"[CryptoFear] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {level} {message}";

    private static void WriteToFile(string line)
    {
        try
        {
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch
        {
            // Silently ignore file write failures
        }
    }
}
