using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoFear.Services;

namespace CryptoFear.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool isDarkMode;

    [ObservableProperty]
    private bool isDebugLogging;

    [ObservableProperty]
    private string selectedCurrencyCode;

    [ObservableProperty]
    private string selectedCurrencyDisplay;

    [ObservableProperty]
    private bool isCurrencyDropdownOpen;

    public List<CurrencyOption> Currencies => CurrencyService.AvailableCurrencies;

    public SettingsViewModel()
    {
        Title = "Settings";
        IsDarkMode = ThemeService.IsDarkMode;
        IsDebugLogging = AppLog.IsEnabled;
        selectedCurrencyCode = CurrencyService.Current.Code;
        selectedCurrencyDisplay = FormatDisplay(CurrencyService.Current);
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        ThemeService.SetDarkMode(value);
    }

    partial void OnIsDebugLoggingChanged(bool value)
    {
        AppLog.IsEnabled = value;
    }

    [RelayCommand]
    private void ToggleCurrencyDropdown()
    {
        IsCurrencyDropdownOpen = !IsCurrencyDropdownOpen;
    }

    [RelayCommand]
    private void SelectCurrency(CurrencyOption currency)
    {
        SelectedCurrencyCode = currency.Code;
        SelectedCurrencyDisplay = FormatDisplay(currency);
        IsCurrencyDropdownOpen = false;
        CurrencyService.SetCurrency(currency.Code);
    }

    private static string FormatDisplay(CurrencyOption c) => $"{c.Symbol}  {c.Code} - {c.Name}";
}
