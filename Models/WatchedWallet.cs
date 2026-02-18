using SQLite;

namespace CryptoFear.Models;

public class WatchedWallet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Label { get; set; } = "";

    public decimal BalanceEth { get; set; }

    public decimal BalanceUsd { get; set; }

    public double Change24h { get; set; }

    public DateTime LastUpdated { get; set; }

    public DateTime CreatedAt { get; set; }
}
