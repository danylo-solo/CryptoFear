using System.Globalization;

namespace CryptoFear.Converters;

public class IntentionToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string intention)
        {
            return intention switch
            {
                "Hold" => Color.FromArgb("#4ADE80"),
                "Sell" => Color.FromArgb("#F87171"),
                _ => Color.FromArgb("#7A7890")
            };
        }

        return Color.FromArgb("#7A7890");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
