using CryptoFear.Models;

namespace CryptoFear.Services;

public interface INewsService
{
    Task<List<NewsArticle>> GetArticlesAsync(bool forceRefresh = false);
    Task RefreshAsync();
}
