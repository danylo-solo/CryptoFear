using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using CryptoFear.Services;

namespace CryptoFear.Controls;

public class FearGreedGauge : ContentView
{
    private readonly SKCanvasView _canvasView;
    private float _animatedValue;
    private float _targetValue;
    private bool _isAnimating;

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(FearGreedGauge), 0d,
            propertyChanged: OnValueChanged);

    public static readonly BindableProperty ClassificationProperty =
        BindableProperty.Create(nameof(Classification), typeof(string), typeof(FearGreedGauge), string.Empty);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Classification
    {
        get => (string)GetValue(ClassificationProperty);
        set => SetValue(ClassificationProperty, value);
    }

    public FearGreedGauge()
    {
        _canvasView = new SKCanvasView();
        _canvasView.PaintSurface += OnPaintSurface;
        Content = _canvasView;
        HeightRequest = 260;
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FearGreedGauge gauge)
        {
            gauge._targetValue = Convert.ToSingle(newValue);
            gauge.AnimateNeedle();
        }
    }

    private void AnimateNeedle()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        var startValue = _animatedValue;
        var animation = new Animation(v =>
        {
            _animatedValue = (float)v;
            _canvasView.InvalidateSurface();
        }, startValue, _targetValue, Easing.CubicOut);

        animation.Commit(this, "NeedleAnimation", 16, 900, finished: (_, _) =>
        {
            _isAnimating = false;
        });
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        float width = info.Width;
        float height = info.Height;
        float centerX = width / 2f;
        float centerY = height * 0.62f;

        // make sure it fits on screen
        float horizontalPadding = width * 0.09f;
        float maxRadiusByWidth = (width - horizontalPadding * 2f) / 2f;
        float maxRadiusByHeight = MathF.Min(centerY - 8f, height * 0.45f);
        float arcRadius = MathF.Min(maxRadiusByWidth, maxRadiusByHeight);

        DrawArc(canvas, centerX, centerY, arcRadius);
        DrawTickMarks(canvas, centerX, centerY, arcRadius);
        DrawNeedle(canvas, centerX, centerY, arcRadius);
        DrawCenterDot(canvas, centerX, centerY);
        DrawValueText(canvas, centerX, centerY, width);
    }

    private void DrawArc(SKCanvas canvas, float cx, float cy, float radius)
    {
        float arcWidth = MathF.Max(14f, radius * 0.1f);
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        var segmentColors = new[]
        {
            new SKColor(0xF8, 0x71, 0x71), // Extreme Fear
            new SKColor(0xFB, 0x92, 0x3C), // Fear
            new SKColor(0xFB, 0xBF, 0x24), // Neutral
            new SKColor(0x4A, 0xDE, 0x80), // Greed
            new SKColor(0x22, 0xC5, 0x5E), // Extreme Greed
        };

        // background track
        using (var baseTrackPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = arcWidth,
            StrokeCap = SKStrokeCap.Round,
            Color = new SKColor(0xE8, 0xE6, 0xF0, 28)
        })
        {
            canvas.DrawArc(rect, 180, 180, false, baseTrackPaint);
        }

        float totalSweep = 180f;
        float segmentAngle = totalSweep / segmentColors.Length;
        float startAngle = 180f;
        float gap = 2.2f;

        for (int i = 0; i < segmentColors.Length; i++)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = arcWidth,
                StrokeCap = SKStrokeCap.Round,
                Color = segmentColors[i]
            };

            float actualStart = startAngle + (i * segmentAngle) + (i > 0 ? gap / 2f : 0);
            float actualSweep = segmentAngle - (i > 0 && i < segmentColors.Length - 1 ? gap : gap / 2f);

            canvas.DrawArc(rect, actualStart, actualSweep, false, paint);
        }
    }

    private void DrawTickMarks(SKCanvas canvas, float cx, float cy, float radius)
    {
        for (int i = 0; i <= 10; i++)
        {
            bool isMajor = i % 5 == 0;
            float angle = 180f + (i * 18f);
            float radians = angle * MathF.PI / 180f;
            float innerR = radius + 10;
            float outerR = radius + (isMajor ? 19 : 14);

            float x1 = cx + innerR * MathF.Cos(radians);
            float y1 = cy + innerR * MathF.Sin(radians);
            float x2 = cx + outerR * MathF.Cos(radians);
            float y2 = cy + outerR * MathF.Sin(radians);

            using var tickPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = isMajor ? 1.8f : 1.2f,
                Color = new SKColor(0xE8, 0xE6, 0xF0, isMajor ? (byte)72 : (byte)44)
            };

            canvas.DrawLine(x1, y1, x2, y2, tickPaint);
        }
    }

    private void DrawNeedle(SKCanvas canvas, float cx, float cy, float radius)
    {
        float normalizedValue = Math.Clamp(_animatedValue / 100f, 0f, 1f);
        float needleAngle = 180f + (normalizedValue * 180f);
        float radians = needleAngle * MathF.PI / 180f;
        float needleLength = radius * 0.75f;

        float tipX = cx + needleLength * MathF.Cos(radians);
        float tipY = cy + needleLength * MathF.Sin(radians);

        // Needle body
        using var needlePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.6f,
            StrokeCap = SKStrokeCap.Round,
            Color = new SKColor(0xE8, 0xE6, 0xF0)
        };
        canvas.DrawLine(cx, cy, tipX, tipY, needlePaint);

        // Tip dot
        using var tipPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue)
        };
        canvas.DrawCircle(tipX, tipY, 5, tipPaint);
    }

    private void DrawCenterDot(SKCanvas canvas, float cx, float cy)
    {
        using var ringPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = new SKColor(0xE8, 0xE6, 0xF0, 36)
        };
        canvas.DrawCircle(cx, cy, 16, ringPaint);

        using var outerPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0x1C, 0x1C, 0x36)
        };
        canvas.DrawCircle(cx, cy, 14, outerPaint);

        using var innerPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue)
        };
        canvas.DrawCircle(cx, cy, 8, innerPaint);
    }

    private void DrawValueText(SKCanvas canvas, float cx, float cy, float width)
    {
        float scaleFactor = Math.Clamp(width / 400f, 0.85f, 1.25f);

        var rounded = Math.Round(_animatedValue, 1, MidpointRounding.AwayFromZero);
        var absFormatted = Math.Abs(rounded).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var parts = absFormatted.Split('.');
        var wholeText = (rounded < 0 ? "-" : string.Empty) + parts[0];
        var decimalText = "." + parts[1];

        using var wholePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue),
            TextSize = 50 * scaleFactor,
            TextAlign = SKTextAlign.Left,
            Typeface = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default
        };

        using var decimalPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue),
            TextSize = 30 * scaleFactor,
            TextAlign = SKTextAlign.Left,
            Typeface = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default
        };

        var wholeWidth = wholePaint.MeasureText(wholeText);
        var decimalWidth = decimalPaint.MeasureText(decimalText);
        var startX = cx - ((wholeWidth + decimalWidth) / 2f);
        var baselineY = cy + 52 * scaleFactor;

        canvas.DrawText(wholeText, startX, baselineY, wholePaint);
        canvas.DrawText(decimalText, startX + wholeWidth, baselineY, decimalPaint);

        // Classification label
        string classification = GetClassification(_animatedValue);
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0x7A, 0x78, 0x90),
            TextSize = 15 * scaleFactor,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default
        };
        canvas.DrawText(classification, cx, cy + 80 * scaleFactor, labelPaint);
    }

    private static SKColor GetColorForValue(float value)
    {
        return value switch
        {
            <= 20 => new SKColor(0xF8, 0x71, 0x71),
            <= 40 => new SKColor(0xFB, 0x92, 0x3C),
            <= 60 => new SKColor(0xFB, 0xBF, 0x24),
            <= 80 => new SKColor(0x4A, 0xDE, 0x80),
            _ => new SKColor(0x22, 0xC5, 0x5E)
        };
    }

    private static string GetClassification(float value)
    {
        return SentimentClassifier.Classify((int)Math.Round(value));
    }
}
