using System.Globalization;
using CryptoFear.Services;

namespace CryptoFear.Converters;

public class FearGreedToClassificationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var numeric = value switch
        {
            int intValue => (double)intValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            _ => double.NaN
        };

        if (!double.IsNaN(numeric))
            return SentimentClassifier.Classify(numeric);

        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
