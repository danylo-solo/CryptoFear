using CryptoFear.Models;

namespace CryptoFear.Services;

public class SentimentCalculator
{
    private const double MomentumWeight = 0.30;
    private const double VolatilityWeight = 0.30;
    private const double VolumeWeight = 0.20;
    private const double CompositionWeight = 0.20;
    private const double EmaAlpha = 0.12;
    private const double ScoreCalibrationOffset = -5.0;
    private const double MaxDailyMove = 4.0;

    public List<ComputedSentimentPoint> ComputeSeries(List<MarketSnapshot> snapshots)
    {
        var ordered = snapshots
            .OrderBy(s => s.DateUtc)
            .ToList();

        if (ordered.Count < 40)
            return new List<ComputedSentimentPoint>();

        var points = new List<ComputedSentimentPoint>();
        double? emaValue = null;

        for (var i = 30; i < ordered.Count; i++)
        {
            var m = ordered[i];

            var momentumScore = ComputeMomentumScore(ordered, i);
            var volatilityScore = ComputeVolatilityScore(ordered, i);
            var volumePressureScore = ComputeVolumePressureScore(ordered, i);
            var compositionScore = ComputeCompositionScore(ordered, i);

            var raw =
                (momentumScore * MomentumWeight) +
                (volatilityScore * VolatilityWeight) +
                (volumePressureScore * VolumeWeight) +
                (compositionScore * CompositionWeight);

            raw = Clamp(raw + ScoreCalibrationOffset, 0, 100);

            var smoothed = emaValue.HasValue
                ? (EmaAlpha * raw) + ((1 - EmaAlpha) * emaValue.Value)
                : raw;

            if (emaValue.HasValue)
            {
                var minAllowed = emaValue.Value - MaxDailyMove;
                var maxAllowed = emaValue.Value + MaxDailyMove;
                smoothed = Clamp(smoothed, minAllowed, maxAllowed);
            }

            emaValue = smoothed;
            var rounded = Math.Round(Clamp(smoothed, 0, 100), 1, MidpointRounding.AwayFromZero);

            points.Add(new ComputedSentimentPoint
            {
                DateKey = m.DateUtc.ToString("yyyy-MM-dd"),
                TimestampUtc = m.DateUtc,
                Value = rounded,
                Classification = SentimentClassifier.Classify(rounded),
                MomentumScore = Round2(momentumScore),
                VolatilityScore = Round2(volatilityScore),
                VolumePressureScore = Round2(volumePressureScore),
                CompositionScore = Round2(compositionScore)
            });
        }

        return points;
    }

    private static double ComputeMomentumScore(IReadOnlyList<MarketSnapshot> data, int i)
    {
        var pNow = data[i].BtcPrice;
        var p7 = data[Math.Max(0, i - 7)].BtcPrice;
        var p30 = data[Math.Max(0, i - 30)].BtcPrice;

        var capNow = data[i].TotalMarketCapProxy;
        var cap7 = data[Math.Max(0, i - 7)].TotalMarketCapProxy;
        var cap30 = data[Math.Max(0, i - 30)].TotalMarketCapProxy;

        var ret7 = SafeReturn(pNow, p7);
        var ret30 = SafeReturn(pNow, p30);
        var capRet7 = SafeReturn(capNow, cap7);
        var capRet30 = SafeReturn(capNow, cap30);

        var blended = (ret7 * 0.45) + (ret30 * 0.25) + (capRet7 * 0.20) + (capRet30 * 0.10);
        return Clamp(50 + (blended * 260), 0, 100);
    }

    private static double ComputeVolatilityScore(IReadOnlyList<MarketSnapshot> data, int i)
    {
        var r30 = Returns(data, i, 30);
        var r90 = Returns(data, i, 90);

        var vol30 = StdDev(r30);
        var vol90 = Math.Max(StdDev(r90), 0.0001);
        var volRatio = vol30 / vol90;

        var drawdown30 = Drawdown(data, i, 30);

        // more vol = more fear
        var volFear = Normalize(volRatio, 0.80, 1.50);
        var ddFear = Normalize(drawdown30, 0.02, 0.25);

        var fearBlend = (volFear * 0.60) + (ddFear * 0.40);
        return Clamp(100 - (fearBlend * 100), 0, 100);
    }

    private static double ComputeVolumePressureScore(IReadOnlyList<MarketSnapshot> data, int i)
    {
        var currentVol = data[i].BtcVolume;
        var avg30 = data
            .Skip(Math.Max(0, i - 29))
            .Take(Math.Min(30, i + 1))
            .Select(x => x.BtcVolume)
            .DefaultIfEmpty(0)
            .Average();

        var ratio = avg30 > 0 ? currentVol / avg30 : 1.0;
        var priceUp = data[i].BtcPrice >= data[Math.Max(0, i - 1)].BtcPrice;

        var pressure = Normalize(ratio, 0.80, 1.60);
        var score = (pressure * 80) + (priceUp ? 10 : -10);
        return Clamp(score, 0, 100);
    }

    private static double ComputeCompositionScore(IReadOnlyList<MarketSnapshot> data, int i)
    {
        var lookback = data
            .Skip(Math.Max(0, i - 89))
            .Take(Math.Min(90, i + 1))
            .ToList();

        var avgBtcShare = lookback.Select(x => x.BtcShare).Average();
        var avgStableShare = lookback.Select(x => x.StablecoinShare).Average();

        var btcDelta = data[i].BtcShare - avgBtcShare;
        var stableDelta = data[i].StablecoinShare - avgStableShare;

        // btc/stable dominance going up usually means fear
        var btcFear = Normalize(btcDelta, -0.05, 0.05);
        var stableFear = Normalize(stableDelta, -0.03, 0.03);
        var fearBlend = (btcFear * 0.60) + (stableFear * 0.40);

        return Clamp(100 - (fearBlend * 100), 0, 100);
    }

    private static List<double> Returns(IReadOnlyList<MarketSnapshot> data, int i, int window)
    {
        var start = Math.Max(1, i - window + 1);
        var list = new List<double>();

        for (var j = start; j <= i; j++)
        {
            var prev = data[j - 1].BtcPrice;
            var current = data[j].BtcPrice;
            if (prev <= 0) continue;
            list.Add((current - prev) / prev);
        }

        return list;
    }

    private static double Drawdown(IReadOnlyList<MarketSnapshot> data, int i, int window)
    {
        var start = Math.Max(0, i - window + 1);
        var slice = data.Skip(start).Take(i - start + 1).ToList();
        if (slice.Count == 0) return 0;

        var peak = slice.Max(x => x.BtcPrice);
        if (peak <= 0) return 0;

        var current = slice.Last().BtcPrice;
        return Math.Max(0, (peak - current) / peak);
    }

    private static double SafeReturn(double current, double previous)
    {
        if (previous <= 0) return 0;
        return (current - previous) / previous;
    }

    private static double StdDev(List<double> values)
    {
        if (values.Count == 0) return 0;
        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        return Math.Sqrt(variance);
    }

    private static double Normalize(double value, double min, double max)
    {
        if (max <= min) return 0.5;
        return Clamp((value - min) / (max - min), 0, 1);
    }

    private static double Clamp(double value, double min, double max)
        => Math.Min(max, Math.Max(min, value));

    private static double Round2(double value) => Math.Round(value, 2);
}
