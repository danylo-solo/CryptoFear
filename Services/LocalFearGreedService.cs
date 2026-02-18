using CryptoFear.Models;

namespace CryptoFear.Services;

public class LocalFearGreedService : IFearGreedService
{
    private readonly RawMarketDataService _rawMarketDataService;
    private readonly SentimentCalculator _calculator;
    private readonly IDataService _dataService;
    private readonly FearGreedService _legacyService;

    public LocalFearGreedService(
        RawMarketDataService rawMarketDataService,
        SentimentCalculator calculator,
        IDataService dataService,
        FearGreedService legacyService)
    {
        _rawMarketDataService = rawMarketDataService;
        _calculator = calculator;
        _dataService = dataService;
        _legacyService = legacyService;
    }

    public async Task<FearGreedEntry?> GetLatestAsync()
    {
        // if local sentiment is turned off just use the api
        if (EnvConfig.Get("USE_LOCAL_SENTIMENT")?.ToLower() == "false")
            return await _legacyService.GetLatestAsync();

        try
        {
            await EnsureFreshLocalDataAsync(90);

            var latest = await _dataService.GetLatestSentimentPointAsync();
            if (latest != null)
            {
                AppLog.Info($"Latest local sentiment loaded. value={latest.Value}, class={latest.Classification}, ts={latest.TimestampUtc:yyyy-MM-dd}");

                if (string.Equals(EnvConfig.Get("COMPARE_LOCAL_SENTIMENT"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    var legacyLatest = await _legacyService.GetLatestAsync();
                    if (legacyLatest != null)
                    {
                        var delta = latest.Value - legacyLatest.Value;
                        AppLog.Info($"Local vs legacy delta={delta} (local={latest.Value}, legacy={legacyLatest.Value})");
                    }
                }

                return new FearGreedEntry
                {
                    Value = latest.Value,
                    Classification = latest.Classification,
                    Timestamp = latest.TimestampUtc
                };
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"Error in local GetLatestAsync: {ex.Message}");
        }

        // fallback if local doesn't work
        AppLog.Info("Falling back to legacy latest sentiment API.");
        return await _legacyService.GetLatestAsync();
    }

    public async Task<List<FearGreedEntry>> GetHistoricalAsync(int days = 365)
    {
        if (EnvConfig.Get("USE_LOCAL_SENTIMENT")?.ToLower() == "false")
            return await _legacyService.GetHistoricalAsync(days);

        try
        {
            await EnsureFreshLocalDataAsync(days);

            var cached = await _dataService.GetSentimentPointsAsync(days);
            if (cached.Count > 0)
            {
                var latestCached = cached.First();
                AppLog.Info($"Historical local sentiment loaded. requestedDays={days}, returned={cached.Count}, latest={latestCached.Value} ({latestCached.Classification})");
                return cached
                    .OrderByDescending(x => x.TimestampUtc)
                    .Select(x => new FearGreedEntry
                    {
                        Value = x.Value,
                        Classification = x.Classification,
                        Timestamp = x.TimestampUtc
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"Error in local GetHistoricalAsync: {ex.Message}");
        }

        AppLog.Info($"Falling back to legacy historical sentiment API. requestedDays={days}");
        return await _legacyService.GetHistoricalAsync(days);
    }

    private async Task EnsureFreshLocalDataAsync(int requestedDays)
    {
        var existing = await _dataService.GetSentimentPointsAsync(requestedDays);
        var latestCached = existing.FirstOrDefault();
        var todayUtc = DateTime.UtcNow.Date;
        var latestCachedDate = latestCached?.TimestampUtc.Date.ToString("yyyy-MM-dd") ?? "none";
        var forceRefresh = string.Equals(EnvConfig.Get("FORCE_SENTIMENT_REFRESH"), "true", StringComparison.OrdinalIgnoreCase);

        AppLog.Info($"Local cache check. requestedDays={requestedDays}, cachedCount={existing.Count}, latestCachedDate={latestCachedDate}, forceRefresh={forceRefresh}");

        // only refresh once a day
        if (!forceRefresh &&
            latestCached != null &&
            latestCached.TimestampUtc.Date >= todayUtc.AddDays(-1) &&
            existing.Count >= Math.Min(requestedDays, 30))
        {
            AppLog.Info("Using cached sentiment data (fresh).");
            return;
        }

        if (forceRefresh)
            AppLog.Info("FORCE_SENTIMENT_REFRESH enabled; bypassing cache.");

        var snapshots = await _rawMarketDataService.GetHistoricalSnapshotsAsync(requestedDays);
        if (snapshots.Count == 0)
        {
            AppLog.Error("No raw snapshots available; skipping local compute.");
            return;
        }

        AppLog.Info($"Computing local sentiment series from snapshots={snapshots.Count}.");

        var computed = _calculator.ComputeSeries(snapshots);
        if (computed.Count == 0)
        {
            AppLog.Error("Sentiment calculator produced zero points.");
            return;
        }

        // filter out bad data
        var cleaned = computed
            .Where(c => c.Value >= 0 && c.Value <= 100)
            .Select(c =>
            {
                if (string.IsNullOrWhiteSpace(c.Classification))
                    c.Classification = SentimentClassifier.Classify(c.Value);
                return c;
            })
            .ToList();

        if (cleaned.Count == 0)
        {
            AppLog.Error("All computed points were out of range.");
            return;
        }

        await _dataService.SaveSentimentPointsAsync(cleaned);
        var latestComputed = cleaned.Last();
        AppLog.Info($"Saved sentiment points={cleaned.Count}. latest={latestComputed.Value} ({latestComputed.Classification}) @ {latestComputed.TimestampUtc:yyyy-MM-dd}");
    }
}
