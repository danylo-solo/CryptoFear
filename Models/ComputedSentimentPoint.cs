using SQLite;

namespace CryptoFear.Models;

public class ComputedSentimentPoint
{
    // YYYY-MM-DD UTC to keep upserts simple and readable.
    [PrimaryKey]
    public string DateKey { get; set; } = "";

    public DateTime TimestampUtc { get; set; }
    public double Value { get; set; }
    public string Classification { get; set; } = string.Empty;

    public double MomentumScore { get; set; }
    public double VolatilityScore { get; set; }
    public double VolumePressureScore { get; set; }
    public double CompositionScore { get; set; }
}
