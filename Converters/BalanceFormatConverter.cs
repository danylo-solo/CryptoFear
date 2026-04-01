using System.Globalization;
using CryptoFear.Services;

namespace CryptoFear.Converters;

public class BalanceFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var symbol = CurrencyService.Current.Symbol;

        if (value is decimal balance)
            return $"{symbol}{balance:N2}";

        return $"{symbol}0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
