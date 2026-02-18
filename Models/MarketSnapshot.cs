namespace CryptoFear.Models;

public class MarketSnapshot
{
    public DateTime DateUtc { get; set; }

    public double BtcPrice { get; set; }
    public double BtcVolume { get; set; }

    public double BtcMarketCap { get; set; }
    public double EthMarketCap { get; set; }
    public double UsdtMarketCap { get; set; }

    // rough total cap from the coins we track
    public double TotalMarketCapProxy { get; set; }

    public double BtcShare { get; set; }
    public double StablecoinShare { get; set; }
}
