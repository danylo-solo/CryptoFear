namespace CryptoFear.Models;

public class WalletBalanceResult
{
    public decimal BalanceEth { get; set; }

    public decimal BalanceUsd { get; set; }

    public double Change24h { get; set; }
}
