using System.Globalization;

namespace CryptoFear.Converters;

public class BalanceFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal balance)
            return balance.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

        return "$0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
