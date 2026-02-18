using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoFear.Models;
using CryptoFear.Services;

namespace CryptoFear.ViewModels;

public partial class WatchViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly WalletService _walletService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWallets))]
    private List<WatchedWallet> wallets = new();

    [ObservableProperty]
    private string addressInput = "";

    [ObservableProperty]
    private string labelInput = string.Empty;

    [ObservableProperty]
    private string addressError = "";

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string selectedTimeframe = "24H";

    public bool HasWallets => Wallets?.Count > 0;

    public WatchViewModel(IDataService dataService, WalletService walletService)
    {
        _dataService = dataService;
        _walletService = walletService;
        Title = "Watch";
    }

    [RelayCommand]
    private async Task LoadWalletsAsync()
    {
        try
        {
            Wallets = await _dataService.GetAllWalletsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading wallets: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddWalletAsync()
    {
        AddressError = string.Empty;

        if (string.IsNullOrWhiteSpace(AddressInput))
        {
            AddressError = "Wallet address is required";
            return;
        }

        var trimmed = AddressInput.Trim();
        if (!trimmed.StartsWith("0x") || trimmed.Length != 42)
        {
            AddressError = "Enter a valid Ethereum address (0x...)";
            return;
        }

        try
        {
            IsBusy = true;

            var balance = await _walletService.GetWalletBalanceAsync(trimmed);

            var wallet = new WatchedWallet
            {
                Address = trimmed,
                Label = string.IsNullOrWhiteSpace(LabelInput) ? ShortenAddress(trimmed) : LabelInput.Trim(),
                BalanceEth = balance?.BalanceEth ?? 0,
                BalanceUsd = balance?.BalanceUsd ?? 0,
                Change24h = balance?.Change24h ?? 0,
                LastUpdated = DateTime.UtcNow
            };

            await _dataService.SaveWalletAsync(wallet);

            AddressInput = string.Empty;
            LabelInput = string.Empty;

            await LoadWalletsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding wallet: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteWalletAsync(WatchedWallet wallet)
    {
        try
        {
            await _dataService.DeleteWalletAsync(wallet);
            await LoadWalletsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting wallet: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshBalancesAsync()
    {
        try
        {
            IsRefreshing = true;

            foreach (var wallet in Wallets)
            {
                var balance = await _walletService.GetWalletBalanceAsync(wallet.Address);
                if (balance != null)
                {
                    wallet.BalanceEth = balance.BalanceEth;
                    wallet.BalanceUsd = balance.BalanceUsd;
                    wallet.Change24h = balance.Change24h;
                    await _dataService.UpdateWalletAsync(wallet);
                }
            }

            await LoadWalletsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing balances: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void SelectTimeframe(string timeframe)
    {
        SelectedTimeframe = timeframe;
    }

    private static string ShortenAddress(string address)
    {
        if (address.Length <= 10) return address;
        return $"{address[..6]}...{address[^4..]}";
    }

    public async Task OnAppearingAsync()
    {
        await LoadWalletsAsync();
    }
}
