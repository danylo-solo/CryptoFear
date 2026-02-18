using System.Text.Json;
using CryptoFear.Models;

namespace CryptoFear.Services;

public class RawMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly string? _coinGeckoApiKey;

    private const string BaseUrl = "https://api.coingecko.com/api/v3";
    private static readonly string[] FixedBasketCoinIds =
    {
        "bitcoin",
        "ethereum",
        "tether",
        "ripple",
        "binancecoin",
        "usd-coin",
        "solana",
        "tron",
        "monero"
    };

    private static readonly HashSet<string> StableCoinIds = new()
    {
        "tether", "usd-coin", "dai", "ethena-usde", "first-digital-usd", "paypal-usd", "usdd", "true-usd", "frax", "gemini-dollar"
    };

    public RawMarketDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _coinGeckoApiKey = EnvConfig.Get("COINGECKO_API_KEY");

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoFear/1.0 (+https://cryptofear.local)");

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<List<MarketSnapshot>> GetHistoricalSnapshotsAsync(int days)
    {
        // Keep requests friendly for free-tier/public endpoint.
        var requestDays = Math.Clamp(Math.Max(days, 180), 180, 365);
        AppLog.Info($"Raw market fetch started. requestedDays={days}, requestDays={requestDays}, basketSize={FixedBasketCoinIds.Length}");

        try
        {
            var historyTasks = FixedBasketCoinIds
                .Select(async id => new
                {
                    Id = id,
                    Data = await GetCoinHistoryAsync(id, requestDays)
                })
                .ToList();

            await Task.WhenAll(historyTasks);

            var histories = historyTasks
                .Select(t => t.Result)
                .Where(x => x.Data.Count > 0)
                .ToDictionary(x => x.Id, x => x.Data);

            AppLog.Info($"Raw histories loaded for {histories.Count}/{FixedBasketCoinIds.Length} coins.");

            if (!histories.TryGetValue("bitcoin", out var btc) || btc.Count == 0)
            {
                AppLog.Error("Raw market fetch failed: missing bitcoin history.");
                return new List<MarketSnapshot>();
            }

            var allDates = histories.Values
                .SelectMany(h => h.Keys)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var snapshots = new List<MarketSnapshot>();
            var lastByCoin = new Dictionary<string, CoinDay>();

            foreach (var date in allDates)
            {
                foreach (var pair in histories)
                {
                    if (pair.Value.TryGetValue(date, out var day))
                        lastByCoin[pair.Key] = day;
                }

                if (!lastByCoin.TryGetValue("bitcoin", out var lastBtc))
                    continue;

                var totalCap = Math.Max(lastByCoin.Values.Sum(v => v.MarketCap), 1d);
                var stableCap = lastByCoin
                    .Where(p => StableCoinIds.Contains(p.Key))
                    .Sum(p => p.Value.MarketCap);

                lastByCoin.TryGetValue("ethereum", out var lastEth);
                lastByCoin.TryGetValue("tether", out var lastUsdt);

                var btcShare = Clamp01(lastBtc.MarketCap / totalCap);
                var stableShare = Clamp01(stableCap / totalCap);

                snapshots.Add(new MarketSnapshot
                {
                    DateUtc = date,
                    BtcPrice = lastBtc.Price,
                    BtcVolume = lastBtc.Volume,
                    BtcMarketCap = lastBtc.MarketCap,
                    EthMarketCap = lastEth?.MarketCap ?? 0,
                    UsdtMarketCap = lastUsdt?.MarketCap ?? 0,
                    TotalMarketCapProxy = totalCap,
                    BtcShare = btcShare,
                    StablecoinShare = stableShare
                });
            }

            var orderedSnapshots = snapshots
                .OrderBy(s => s.DateUtc)
                .ToList();

            if (orderedSnapshots.Count > 0)
            {
                var first = orderedSnapshots.First();
                var last = orderedSnapshots.Last();
                AppLog.Info(
                    $"Raw market snapshots ready. count={orderedSnapshots.Count}, range={first.DateUtc:yyyy-MM-dd}..{last.DateUtc:yyyy-MM-dd}, " +
                    $"lastTotalCap={last.TotalMarketCapProxy:F0}, lastBtcShare={last.BtcShare:P2}, lastStableShare={last.StablecoinShare:P2}");
            }
            else
            {
                AppLog.Info("Raw market snapshots ready but empty.");
            }

            return orderedSnapshots;
        }
        catch (Exception ex)
        {
            AppLog.Error($"Error fetching raw market snapshots: {ex.Message}");
            return new List<MarketSnapshot>();
        }
    }

    private async Task<Dictionary<DateTime, CoinDay>> GetCoinHistoryAsync(string coinId, int days)
    {
        var url = $"{BaseUrl}/coins/{coinId}/market_chart?vs_currency=usd&days={days}&interval=daily";
        var json = await GetJsonAsync(url);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var prices = ReadTimeSeries(root, "prices");
        var caps = ReadTimeSeries(root, "market_caps");
        var vols = ReadTimeSeries(root, "total_volumes");

        var dates = prices.Keys
            .Union(caps.Keys)
            .Union(vols.Keys)
            .OrderBy(d => d);

        var result = new Dictionary<DateTime, CoinDay>();

        foreach (var date in dates)
        {
            prices.TryGetValue(date, out var p);
            caps.TryGetValue(date, out var c);
            vols.TryGetValue(date, out var v);

            result[date] = new CoinDay
            {
                Price = p,
                MarketCap = c,
                Volume = v
            };
        }

        return result;
    }

    private async Task<string> GetJsonAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyCoinGeckoAuthHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var snippet = body.Length > 220 ? body[..220] + "..." : body;
            throw new HttpRequestException($"CoinGecko request failed ({(int)response.StatusCode} {response.StatusCode}). url={url}. body={snippet}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private void ApplyCoinGeckoAuthHeaders(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_coinGeckoApiKey))
            return;

        request.Headers.TryAddWithoutValidation("x-cg-demo-api-key", _coinGeckoApiKey);
    }

    private static Dictionary<DateTime, double> ReadTimeSeries(JsonElement root, string name)
    {
        var output = new Dictionary<DateTime, double>();
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return output;

        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
                continue;

            var ts = item[0].GetDouble();
            var val = item[1].GetDouble();
            var date = DateTimeOffset.FromUnixTimeMilliseconds((long)ts).UtcDateTime.Date;
            output[date] = val;
        }

        return output;
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));

    private class CoinDay
    {
        public double Price { get; set; }
        public double MarketCap { get; set; }
        public double Volume { get; set; }
    }
}
