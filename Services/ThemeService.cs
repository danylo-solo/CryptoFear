namespace CryptoFear.Services;

public static class ThemeService
{
    private const string ThemePreferenceKey = "app_theme_dark";

    public static bool IsDarkMode { get; private set; } = true;

    public static event Action? ThemeChanged;

    public static void Initialize()
    {
        IsDarkMode = Preferences.Get(ThemePreferenceKey, true);
        ApplyTheme();
    }

    public static void SetDarkMode(bool isDark)
    {
        if (IsDarkMode == isDark) return;
        IsDarkMode = isDark;
        Preferences.Set(ThemePreferenceKey, isDark);
        ApplyTheme();
        ThemeChanged?.Invoke();
    }

    private static void ApplyTheme()
    {
        var res = Application.Current?.Resources;
        if (res == null) return;

        var colors = IsDarkMode ? DarkColors : LightColors;
        foreach (var (key, color) in colors)
            res[key] = color;

        UpdateBrush(res, "PrimaryBrush", "Primary");
        UpdateBrush(res, "PrimaryDarkBrush", "PrimaryDark");
        UpdateBrush(res, "PrimaryLightBrush", "PrimaryLight");
        UpdateBrush(res, "SurfaceBrush", "Surface");
        UpdateBrush(res, "SurfaceElevatedBrush", "SurfaceElevated");
        UpdateBrush(res, "BackgroundBrush", "Background");

        UpdateGradientBrushes(res);
    }

    private static void UpdateBrush(ResourceDictionary res, string brushKey, string colorKey)
    {
        if (res.TryGetValue(colorKey, out var obj) && obj is Color color)
            res[brushKey] = new SolidColorBrush(color);
    }

    private static void UpdateGradientBrushes(ResourceDictionary res)
    {
        var heroStart = IsDarkMode ? Color.FromArgb("#1A1840") : Color.FromArgb("#E8E4F5");
        var heroEnd = IsDarkMode ? Color.FromArgb("#0E0E18") : Color.FromArgb("#F0EFF7");
        res["HeroGradientBrush"] = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0.5, 1),
            GradientStops = new GradientStopCollection
            {
                new(heroStart, 0.0f),
                new(heroEnd, 1.0f)
            }
        };

        if (res.TryGetValue("Surface", out var s) && s is Color surface
            && res.TryGetValue("SurfaceElevated", out var se) && se is Color surfaceElev)
        {
            res["CardGradientBrush"] = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new(surface, 0.0f),
                    new(surfaceElev, 1.0f)
                }
            };
        }
    }

    private static readonly Dictionary<string, Color> DarkColors = new()
    {
        ["Background"] = Color.FromArgb("#08080F"),
        ["BackgroundAlt"] = Color.FromArgb("#0E0E18"),
        ["Surface"] = Color.FromArgb("#14142A"),
        ["SurfaceElevated"] = Color.FromArgb("#1C1C36"),
        ["SurfaceContainer"] = Color.FromArgb("#242444"),
        ["Primary"] = Color.FromArgb("#8B7EC8"),
        ["PrimaryDark"] = Color.FromArgb("#6B5FA8"),
        ["PrimaryLight"] = Color.FromArgb("#B3A6E0"),
        ["PrimaryContainer"] = Color.FromArgb("#2A2450"),
        ["TextPrimary"] = Color.FromArgb("#E8E6F0"),
        ["TextSecondary"] = Color.FromArgb("#7A7890"),
        ["TextMuted"] = Color.FromArgb("#4A4860"),
        ["Border"] = Color.FromArgb("#2A2A44"),
        ["BorderLight"] = Color.FromArgb("#363654"),
        ["Gray100"] = Color.FromArgb("#1C1C30"),
        ["Gray200"] = Color.FromArgb("#2A2A44"),
        ["Gray300"] = Color.FromArgb("#3A3A58"),
        ["Gray400"] = Color.FromArgb("#4A4860"),
        ["Gray500"] = Color.FromArgb("#5A5878"),
        ["Gray600"] = Color.FromArgb("#7A7890"),
        ["Gray700"] = Color.FromArgb("#9A98B0"),
        ["Gray800"] = Color.FromArgb("#BAB8D0"),
        ["Gray900"] = Color.FromArgb("#E8E6F0"),
        ["ShadowColor"] = Color.FromArgb("#000000"),
    };

    private static readonly Dictionary<string, Color> LightColors = new()
    {
        ["Background"] = Color.FromArgb("#F8F7FC"),
        ["BackgroundAlt"] = Color.FromArgb("#F0EFF7"),
        ["Surface"] = Color.FromArgb("#FFFFFF"),
        ["SurfaceElevated"] = Color.FromArgb("#F5F4FA"),
        ["SurfaceContainer"] = Color.FromArgb("#EBE9F5"),
        ["Primary"] = Color.FromArgb("#7B6DB8"),
        ["PrimaryDark"] = Color.FromArgb("#5B4F98"),
        ["PrimaryLight"] = Color.FromArgb("#A396D8"),
        ["PrimaryContainer"] = Color.FromArgb("#EDE9F7"),
        ["TextPrimary"] = Color.FromArgb("#1A1A2E"),
        ["TextSecondary"] = Color.FromArgb("#5A5878"),
        ["TextMuted"] = Color.FromArgb("#9A98B0"),
        ["Border"] = Color.FromArgb("#D8D6E5"),
        ["BorderLight"] = Color.FromArgb("#C8C6D5"),
        ["Gray100"] = Color.FromArgb("#F0EFF7"),
        ["Gray200"] = Color.FromArgb("#E5E4EE"),
        ["Gray300"] = Color.FromArgb("#D5D4DE"),
        ["Gray400"] = Color.FromArgb("#9A98B0"),
        ["Gray500"] = Color.FromArgb("#7A7890"),
        ["Gray600"] = Color.FromArgb("#5A5878"),
        ["Gray700"] = Color.FromArgb("#4A4860"),
        ["Gray800"] = Color.FromArgb("#3A3A58"),
        ["Gray900"] = Color.FromArgb("#1A1A2E"),
        ["ShadowColor"] = Color.FromArgb("#8886A0"),
    };
}
