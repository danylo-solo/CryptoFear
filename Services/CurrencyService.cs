namespace CryptoFear.Services;

public record CurrencyOption(string Code, string Symbol, string Name);

public static class CurrencyService
{
    private const string CurrencyPreferenceKey = "app_currency";

    public static readonly List<CurrencyOption> AvailableCurrencies = new()
    {
        new("USD", "$",  "US Dollar"),
        new("EUR", "€",  "Euro"),
        new("GBP", "£",  "British Pound"),
        new("JPY", "¥",  "Japanese Yen"),
        new("CNY", "¥",  "Chinese Yuan"),
        new("KRW", "₩",  "South Korean Won"),
        new("INR", "₹",  "Indian Rupee"),
        new("BRL", "R$", "Brazilian Real"),
        new("CAD", "C$", "Canadian Dollar"),
        new("AUD", "A$", "Australian Dollar"),
    };

    public static CurrencyOption Current { get; private set; } = AvailableCurrencies[0];

    public static event Action? CurrencyChanged;

    public static void Initialize()
    {
        var saved = Preferences.Get(CurrencyPreferenceKey, "USD");
        Current = AvailableCurrencies.Find(c => c.Code == saved) ?? AvailableCurrencies[0];
    }

    public static void SetCurrency(string code)
    {
        var match = AvailableCurrencies.Find(c => c.Code == code);
        if (match == null || match.Code == Current.Code) return;

        Current = match;
        Preferences.Set(CurrencyPreferenceKey, code);
        CurrencyChanged?.Invoke();
    }
}
