using System.Globalization;

namespace CryptoFear.Converters;

public class FearGreedToColorConverter : IValueConverter
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
        {
            return numeric switch
            {
                <= 20 => Color.FromArgb("#F87171"),
                <= 40 => Color.FromArgb("#FB923C"),
                <= 60 => Color.FromArgb("#FBBF24"),
                <= 80 => Color.FromArgb("#4ADE80"),
                _ => Color.FromArgb("#22C55E")
            };
        }

        return Color.FromArgb("#7A7890");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
