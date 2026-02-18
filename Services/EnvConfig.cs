namespace CryptoFear.Services;

public static class EnvConfig
{
    private static readonly Dictionary<string, string> _envVars = new();
    private static bool _loaded = false;

    public static void Load()
    {
        if (_loaded) return;

        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(".env").Result;
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                ParseEnvContent(content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load .env from assets: {ex.Message}");
            TryLoadFromFileSystem();
        }

        _loaded = true;
    }

    private static void TryLoadFromFileSystem()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ".env");
        try
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                ParseEnvContent(content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read .env: {ex.Message}");
        }
    }

    private static void ParseEnvContent(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex > 0)
            {
                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();

                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                _envVars[key] = value;
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static string? Get(string key)
    {
        Load();

        if (_envVars.TryGetValue(key, out var value))
            return value;

        return Environment.GetEnvironmentVariable(key);
    }

    public static string? EtherscanApiKey => Get("ETHERSCAN_API_KEY");
}
