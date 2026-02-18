using System.Globalization;

namespace CryptoFear.Converters;

public class PercentageToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return percentage >= 0
                ? Color.FromArgb("#4ADE80")  // Success green
                : Color.FromArgb("#F87171"); // Danger red
        }

        return Color.FromArgb("#7A7890"); // TextSecondary
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
