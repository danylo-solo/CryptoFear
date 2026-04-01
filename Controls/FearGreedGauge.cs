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
        HeightRequest = 220;
        ThemeService.ThemeChanged += () => _canvasView.InvalidateSurface();
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FearGreedGauge gauge)
        {
            gauge._targetValue = Convert.ToSingle(newValue);
            gauge.AnimateIndicator();
        }
    }

    private void AnimateIndicator()
    {
        this.AbortAnimation("IndicatorAnimation");

        var startValue = _animatedValue;
        var clampedTarget = Math.Clamp(_targetValue, 0f, 100f);
        var delta = Math.Abs(clampedTarget - startValue);
        uint duration = (uint)Math.Clamp(380 + (delta * 8), 380, 950);

        var animation = new Animation(v =>
        {
            _animatedValue = (float)v;
            _canvasView.InvalidateSurface();
        }, startValue, clampedTarget, Easing.SinOut);

        animation.Commit(this, "IndicatorAnimation", 16, duration, finished: (_, _) =>
        {
            _animatedValue = clampedTarget;
            _canvasView.InvalidateSurface();
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
        float centerY = height * 0.54f;

        float horizontalPadding = width * 0.12f;
        float maxRadius = (width - horizontalPadding * 2f) / 2f;
        float arcRadius = MathF.Min(maxRadius, height * 0.44f);

        DrawArc(canvas, centerX, centerY, arcRadius);
        DrawIndicatorDot(canvas, centerX, centerY, arcRadius);
        DrawValueText(canvas, centerX, centerY, arcRadius, width);
    }

    private static readonly SKColor[] SegmentColors =
    {
        new(0xF8, 0x71, 0x71),
        new(0xFB, 0x92, 0x3C),
        new(0xFB, 0xBF, 0x24),
        new(0x4A, 0xDE, 0x80),
        new(0x22, 0xC5, 0x5E),
    };

    private void DrawArc(SKCanvas canvas, float cx, float cy, float radius)
    {
        float arcWidth = MathF.Max(10f, radius * 0.07f);
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        float totalSweep = 180f;
        float segmentAngle = totalSweep / SegmentColors.Length;
        float gap = 2.5f;

        for (int i = 0; i < SegmentColors.Length; i++)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = arcWidth,
                StrokeCap = SKStrokeCap.Round,
                Color = SegmentColors[i]
            };

            float actualStart = 180f + (i * segmentAngle) + (i > 0 ? gap / 2f : 0);
            float actualSweep = segmentAngle - (i > 0 && i < SegmentColors.Length - 1 ? gap : gap / 2f);

            canvas.DrawArc(rect, actualStart, actualSweep, false, paint);
        }
    }

    private void DrawIndicatorDot(SKCanvas canvas, float cx, float cy, float radius)
    {
        float normalizedValue = Math.Clamp(_animatedValue / 100f, 0f, 1f);
        float radians = (180f + normalizedValue * 180f) * MathF.PI / 180f;

        float dotX = cx + radius * MathF.Cos(radians);
        float dotY = cy + radius * MathF.Sin(radians);

        var isDark = ThemeService.IsDarkMode;

        using var basePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = isDark ? new SKColor(0x08, 0x08, 0x0F) : new SKColor(0xF8, 0xF7, 0xFC)
        };
        canvas.DrawCircle(dotX, dotY, 13f, basePaint);

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = isDark ? new SKColor(0x14, 0x14, 0x2A) : new SKColor(0xFF, 0xFF, 0xFF)
        };
        canvas.DrawCircle(dotX, dotY, 10f, fillPaint);

        using var accentPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue)
        };
        canvas.DrawCircle(dotX, dotY, 5.5f, accentPaint);
    }

    private void DrawValueText(SKCanvas canvas, float cx, float cy, float radius, float width)
    {
        float scaleFactor = Math.Clamp(width / 400f, 0.85f, 1.25f);

        var whole = ((int)Math.Round(_animatedValue)).ToString();

        using var valuePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = ThemeService.IsDarkMode ? new SKColor(0xE8, 0xE6, 0xF0) : new SKColor(0x1A, 0x1A, 0x2E),
            TextSize = 54 * scaleFactor,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default
        };

        float baselineY = cy - radius * 0.15f;
        canvas.DrawText(whole, cx, baselineY, valuePaint);

        string classification = GetClassification(_animatedValue);
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GetColorForValue(_animatedValue).WithAlpha(200),
            TextSize = 15 * scaleFactor,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default
        };
        canvas.DrawText(classification, cx, baselineY + 24 * scaleFactor, labelPaint);
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
