namespace CryptoFear.Services;

public static class SentimentClassifier
{
    public static string Classify(int value) => value switch
    {
        <= 24 => "Extreme Fear",
        <= 49 => "Fear",
        50 => "Neutral",
        <= 74 => "Greed",
        _ => "Extreme Greed"
    };

    public static string Classify(double value)
        => Classify((int)Math.Round(value, MidpointRounding.AwayFromZero));
}
