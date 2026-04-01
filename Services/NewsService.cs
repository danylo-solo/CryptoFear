using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CryptoFear.Models;

namespace CryptoFear.Services;

public partial class NewsService : INewsService
{
    private readonly HttpClient _httpClient;
    private readonly IDataService _dataService;
    private DateTime _lastRefresh = DateTime.MinValue;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private static readonly (string Url, string Source)[] Feeds =
    [
        ("https://cointelegraph.com/rss", "CoinTelegraph"),
        ("https://www.coindesk.com/arc/outboundfeeds/rss/", "CoinDesk"),
        ("https://bitcoinmagazine.com/feed", "Bitcoin Magazine"),
        ("https://decrypt.co/feed", "Decrypt"),
        ("https://cryptonews.com/news/feed/", "CryptoNews")
    ];

    public NewsService(HttpClient httpClient, IDataService dataService)
    {
        _httpClient = httpClient;
        _dataService = dataService;
    }

    public async Task<List<NewsArticle>> GetArticlesAsync(bool forceRefresh = false)
    {
        var cacheExpired = DateTime.UtcNow - _lastRefresh > CacheLifetime;

        if (forceRefresh || cacheExpired)
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Refresh failed, falling back to cache: {ex.Message}");
            }
        }

        return await _dataService.GetCachedArticlesAsync();
    }

    public async Task RefreshAsync()
    {
        var tasks = Feeds.Select(f => FetchFeedAsync(f.Url, f.Source));
        var results = await Task.WhenAll(tasks);

        var allArticles = results
            .SelectMany(r => r)
            .GroupBy(a => a.Url)
            .Select(g => g.First())
            .OrderByDescending(a => a.PublishedDate)
            .Take(50)
            .ToList();

        if (allArticles.Count > 0)
        {
            await _dataService.SaveArticlesAsync(allArticles);
            await _dataService.PurgeOldArticlesAsync();
        }

        _lastRefresh = DateTime.UtcNow;
    }

    private async Task<List<NewsArticle>> FetchFeedAsync(string url, string sourceName)
    {
        var articles = new List<NewsArticle>();

        try
        {
            var xml = await _httpClient.GetStringAsync(url);
            var doc = XDocument.Parse(xml);
            var items = doc.Descendants("item");

            foreach (var item in items)
            {
                var title = item.Element("title")?.Value?.Trim() ?? "";
                var link = item.Element("link")?.Value?.Trim() ?? "";
                var description = item.Element("description")?.Value?.Trim() ?? "";
                var pubDateStr = item.Element("pubDate")?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                    continue;

                var pubDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(pubDateStr))
                {
                    if (DateTimeOffset.TryParse(pubDateStr, out var parsed))
                    {
                        pubDate = parsed.UtcDateTime;
                    }
                    else if (DateTimeOffset.TryParseExact(pubDateStr,
                        ["ddd, dd MMM yyyy HH:mm:ss zzz", "ddd, dd MMM yyyy HH:mm:ss Z"],
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedExact))
                    {
                        pubDate = parsedExact.UtcDateTime;
                    }
                }

                articles.Add(new NewsArticle
                {
                    Title = StripHtml(title),
                    Summary = TruncateSummary(StripHtml(description), 200),
                    Source = sourceName,
                    PublishedDate = pubDate,
                    Url = link
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching RSS from {sourceName}: {ex.Message}");
        }

        return articles;
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var decoded = System.Net.WebUtility.HtmlDecode(input);
        return HtmlTagRegex().Replace(decoded, "").Trim();
    }

    private static string TruncateSummary(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
            truncated = truncated[..lastSpace];

        return truncated + "...";
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
