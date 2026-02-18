using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoFear.Models;

namespace CryptoFear.Services;

public class WalletService
{
    private readonly HttpClient _httpClient;
    private const string EtherscanBaseUrl = "https://api.etherscan.io/api";
    private const string CoinGeckoBaseUrl = "https://api.coingecko.com/api/v3";

    public WalletService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WalletBalanceResult?> GetWalletBalanceAsync(string address)
    {
        try
        {
            var balanceWei = await FetchEthBalanceAsync(address);
            if (balanceWei < 0) return null;

            var balanceEth = balanceWei / 1_000_000_000_000_000_000m;
            var priceData = await FetchEthPriceAsync();

            return new WalletBalanceResult
            {
                BalanceEth = balanceEth,
                BalanceUsd = balanceEth * (priceData?.PriceUsd ?? 0),
                Change24h = priceData?.Change24h ?? 0
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching wallet balance: {ex.Message}");
            return null;
        }
    }

    private async Task<decimal> FetchEthBalanceAsync(string address)
    {
        try
        {
            var apiKey = EnvConfig.EtherscanApiKey ?? "";
            var url = $"{EtherscanBaseUrl}?module=account&action=balance&address={address}&tag=latest&apikey={apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<EtherscanResponse>(response);

            if (result?.Status == "1" && decimal.TryParse(result.Result, out var wei))
                return wei;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching ETH balance: {ex.Message}");
        }

        return -1;
    }

    private async Task<EthPriceData?> FetchEthPriceAsync()
    {
        try
        {
            var url = $"{CoinGeckoBaseUrl}/simple/price?ids=ethereum&vs_currencies=usd&include_24hr_change=true";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<CoinGeckoResponse>(response);

            if (result?.Ethereum != null)
            {
                return new EthPriceData
                {
                    PriceUsd = result.Ethereum.Usd,
                    Change24h = result.Ethereum.UsdChange24h
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching ETH price: {ex.Message}");
        }

        return null;
    }

    private class EtherscanResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("result")]
        public string Result { get; set; } = "0";
    }

    private class CoinGeckoResponse
    {
        [JsonPropertyName("ethereum")]
        public CoinGeckoEth? Ethereum { get; set; }
    }

    private class CoinGeckoEth
    {
        [JsonPropertyName("usd")]
        public decimal Usd { get; set; }

        [JsonPropertyName("usd_24h_change")]
        public double UsdChange24h { get; set; }
    }

    private class EthPriceData
    {
        public decimal PriceUsd { get; set; }
        public double Change24h { get; set; }
    }
}
