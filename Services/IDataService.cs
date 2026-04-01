using CryptoFear.Models;

namespace CryptoFear.Services;

public interface IDataService
{
    Task InitializeDatabaseAsync();

    // UserEntry
    Task<List<UserEntry>> GetAllEntriesAsync();
    Task<UserEntry?> GetEntryByIdAsync(int id);
    Task<int> SaveEntryAsync(UserEntry entry);
    Task<int> DeleteEntryAsync(UserEntry entry);

    // WatchedWallet
    Task<List<WatchedWallet>> GetAllWalletsAsync();
    Task<int> SaveWalletAsync(WatchedWallet wallet);
    Task<int> UpdateWalletAsync(WatchedWallet wallet);
    Task<int> DeleteWalletAsync(WatchedWallet wallet);

    // Computed sentiment cache
    Task<List<ComputedSentimentPoint>> GetSentimentPointsAsync(int days);
    Task<ComputedSentimentPoint?> GetLatestSentimentPointAsync();
    Task SaveSentimentPointsAsync(IEnumerable<ComputedSentimentPoint> points);

    // NewsArticle cache
    Task<List<NewsArticle>> GetCachedArticlesAsync(int limit = 50);
    Task SaveArticlesAsync(IEnumerable<NewsArticle> articles);
    Task PurgeOldArticlesAsync(int keepDays = 7);
}
