using System.Diagnostics;

namespace CryptoFear.Services;

public static class AppLog
{
    public static void Info(string message)
    {
        if (EnvConfig.Get("SENTIMENT_DEBUG_LOG")?.ToLower() != "true") return;
        Debug.WriteLine($"[CryptoFear] {DateTime.UtcNow:HH:mm:ss} INFO  {message}");
    }

    public static void Error(string message)
    {
        Debug.WriteLine($"[CryptoFear] {DateTime.UtcNow:HH:mm:ss} ERROR {message}");
    }
}
