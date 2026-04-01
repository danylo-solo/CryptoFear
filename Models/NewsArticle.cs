using SQLite;

namespace CryptoFear.Models;

public class NewsArticle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }

    [Indexed]
    public string Url { get; set; } = string.Empty;

    public DateTime CachedAt { get; set; }
}
