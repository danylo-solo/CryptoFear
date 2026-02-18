using CryptoFear.Models;

namespace CryptoFear.Services;

public interface IFearGreedService
{
    Task<FearGreedEntry?> GetLatestAsync();
    Task<List<FearGreedEntry>> GetHistoricalAsync(int days = 365);
}
