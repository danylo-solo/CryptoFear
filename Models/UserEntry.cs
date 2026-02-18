using SQLite;
using System.Globalization;

namespace CryptoFear.Models;

public class UserEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Balance { get; set; }

    public string Intention { get; set; } = "Hold";

    public DateTime EntryDate { get; set; }

    public double FearGreedValue { get; set; }

    [Ignore]
    public string FearGreedValueWhole
    {
        get
        {
            var rounded = Math.Round(FearGreedValue, 1, MidpointRounding.AwayFromZero);
            return Math.Truncate(rounded).ToString("0", CultureInfo.InvariantCulture);
        }
    }

    [Ignore]
    public string FearGreedValueDecimal
    {
        get
        {
            var rounded = Math.Round(Math.Abs(FearGreedValue), 1, MidpointRounding.AwayFromZero);
            var formatted = rounded.ToString("0.0", CultureInfo.InvariantCulture);
            return "." + formatted.Split('.')[1];
        }
    }

    public DateTime CreatedAt { get; set; }
}
