using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoFear.Models;

namespace CryptoFear.Services;

public class FearGreedService : IFearGreedService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.alternative.me/fng/";

    public FearGreedService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FearGreedEntry?> GetLatestAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{BaseUrl}?limit=1&format=json");
            var result = JsonSerializer.Deserialize<ApiResponse>(response);

            if (result?.Data is { Count: > 0 })
            {
                var item = result.Data[0];
                return new FearGreedEntry
                {
                    Value = double.Parse(item.Value, CultureInfo.InvariantCulture),
                    Classification = item.ValueClassification,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.Timestamp)).DateTime
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching latest fear & greed: {ex.Message}");
        }

        return null;
    }

    public async Task<List<FearGreedEntry>> GetHistoricalAsync(int days = 365)
    {
        var entries = new List<FearGreedEntry>();

        try
        {
            var response = await _httpClient.GetStringAsync($"{BaseUrl}?limit={days}&format=json");
            var result = JsonSerializer.Deserialize<ApiResponse>(response);

            if (result?.Data != null)
            {
                foreach (var item in result.Data)
                {
                    entries.Add(new FearGreedEntry
                    {
                        Value = double.Parse(item.Value, CultureInfo.InvariantCulture),
                        Classification = item.ValueClassification,
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.Timestamp)).DateTime
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching historical fear & greed: {ex.Message}");
        }

        return entries;
    }

    private class ApiResponse
    {
        [JsonPropertyName("data")]
        public List<ApiDataItem> Data { get; set; } = new();
    }

    private class ApiDataItem
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = "0";

        [JsonPropertyName("value_classification")]
        public string ValueClassification { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "0";
    }
}
