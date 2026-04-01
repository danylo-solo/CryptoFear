using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoFear.Models;
using CryptoFear.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Globalization;

namespace CryptoFear.ViewModels;

public partial class ChartViewModel : BaseViewModel
{
    private readonly IFearGreedService _fearGreedService;
    private readonly IDataService _dataService;
    private List<FearGreedEntry> _cachedHistorical = new();

    [ObservableProperty]
    private ISeries[] series = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] yAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Margin? drawMargin = new Margin(12, 10, 12, 10);

    [ObservableProperty]
    private string balanceText = "";

    [ObservableProperty]
    private string selectedIntention = "Hold";

    [ObservableProperty]
    private DateTime entryDate = DateTime.Today;

    [ObservableProperty]
    private string balanceError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEntries))]
    private List<UserEntry> allEntries = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEntry))]
    private UserEntry? selectedEntry;

    // CMC-style summary stats
    [ObservableProperty]
    private double yesterdayValue;

    [ObservableProperty]
    private string yesterdayClassification = "";

    [ObservableProperty]
    private double lastWeekValue;

    [ObservableProperty]
    private string lastWeekClassification = string.Empty;

    [ObservableProperty]
    private double lastMonthValue;

    [ObservableProperty]
    private string lastMonthClassification = "";

    [ObservableProperty]
    private string selectedChartRange = "1Y";

    [ObservableProperty]
    private bool isDatePickerExpanded;

    public bool HasEntries => AllEntries?.Count > 0;
    public bool HasSelectedEntry => SelectedEntry != null;

    public List<string> Intentions { get; } = new() { "Hold", "Sell" };

    public event EventHandler? SaveSucceeded;

    public ChartViewModel(IFearGreedService fearGreedService, IDataService dataService)
    {
        _fearGreedService = fearGreedService;
        _dataService = dataService;
        Title = "Chart";
    }

    public string YesterdayValueWhole => GetWholePart(YesterdayValue);
    public string YesterdayValueDecimal => GetDecimalPart(YesterdayValue);
    public string LastWeekValueWhole => GetWholePart(LastWeekValue);
    public string LastWeekValueDecimal => GetDecimalPart(LastWeekValue);
    public string LastMonthValueWhole => GetWholePart(LastMonthValue);
    public string LastMonthValueDecimal => GetDecimalPart(LastMonthValue);

    private static int GetDaysForRange(string range) => range switch
    {
        "7D" => 7,
        "30D" => 30,
        "90D" => 90,
        "1Y" => 365,
        _ => 365
    };

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var days = GetDaysForRange(SelectedChartRange);

            var historicalTask = _fearGreedService.GetHistoricalAsync(days);
            var entriesTask = _dataService.GetAllEntriesAsync();

            await Task.WhenAll(historicalTask, entriesTask);

            var historical = await historicalTask;
            _cachedHistorical = historical;
            AllEntries = await entriesTask;

            DeriveStats(historical);
            BuildChart(historical, AllEntries);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading chart data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void DeriveStats(List<FearGreedEntry> historical)
    {
        if (historical.Count == 0) return;

        var ordered = historical.OrderByDescending(h => h.Timestamp).ToList();

        FearGreedEntry? FindClosest(DateTime target) =>
            ordered.MinBy(h => Math.Abs((h.Timestamp.Date - target.Date).TotalDays));

        var yesterday = FindClosest(DateTime.UtcNow.Date.AddDays(-1));
        var lastWeek = FindClosest(DateTime.UtcNow.Date.AddDays(-7));
        var lastMonth = FindClosest(DateTime.UtcNow.Date.AddDays(-30));

        if (yesterday != null) { YesterdayValue = yesterday.Value; YesterdayClassification = SentimentClassifier.Classify(yesterday.Value); }
        if (lastWeek != null) { LastWeekValue = lastWeek.Value; LastWeekClassification = SentimentClassifier.Classify(lastWeek.Value); }
        if (lastMonth != null) { LastMonthValue = lastMonth.Value; LastMonthClassification = SentimentClassifier.Classify(lastMonth.Value); }
    }

    private void BuildChart(List<FearGreedEntry> historical, List<UserEntry> entries)
    {
        var ordered = historical.OrderBy(h => h.Timestamp).ToList();
        if (ordered.Count == 0) return;

        var seriesList = new List<ISeries>();

        // X bounds are always driven by the historical data range — never by entries
        var minDate = ordered.First().Timestamp;
        var maxDate = ordered.Last().Timestamp;

        // Only render markers that fall within the currently displayed date range
        var rangeStart = minDate.Date;
        var rangeEnd = maxDate.Date;
        var visibleEntries = entries
            .Where(e => e.EntryDate.Date >= rangeStart && e.EntryDate.Date <= rangeEnd)
            .ToList();

        var rangeTicks = (maxDate - minDate).Ticks;
        if (rangeTicks <= 0) rangeTicks = TimeSpan.FromDays(1).Ticks;
        var paddingTicks = (long)(rangeTicks * 0.02);
        var xMin = (double)(minDate.Ticks - paddingTicks);
        var xMax = (double)(maxDate.Ticks + paddingTicks);

        // main chart line
        var lineValues = ordered
            .Select(h => new DateTimePoint(h.Timestamp, h.Value))
            .ToList();

        const int primaryR = 0x8B, primaryG = 0x7E, primaryB = 0xC8;
        seriesList.Add(new LineSeries<DateTimePoint>
        {
            Values = lineValues,
            GeometrySize = 0,
            Stroke = new SolidColorPaint(new SKColor(0xD0, 0xCE, 0xE0), 2.2f),
            Fill = new SolidColorPaint(new SKColor((byte)primaryR, (byte)primaryG, (byte)primaryB, 28)),
            LineSmoothness = 0.4
        });

        // Build a date → value lookup from historical data so dots snap to the line
        var historicalLookup = ordered.ToDictionary(h => h.Timestamp.Date, h => h.Value);

        // Resolve the historical Y-value for any entry date
        double ResolveY(DateTime date)
        {
            var utcDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            if (historicalLookup.TryGetValue(utcDate.Date, out var v)) return v;
            return ordered.MinBy(h => Math.Abs((h.Timestamp.Date - utcDate.Date).TotalDays))?.Value ?? 0;
        }

        // user entries as dots — only those within the active date range
        var entryHistoricalValues = visibleEntries.Select(e => ResolveY(e.EntryDate)).ToList();
        if (visibleEntries.Count > 0)
        {
            seriesList.Add(new ScatterSeries<UserEntry>
            {
                Values = visibleEntries,
                // Coordinate(x, y) — x = SecondaryValue (date ticks), y = PrimaryValue (F&G value)
                Mapping = (e, index) =>
                {
                    var utcDate = new DateTime(e.EntryDate.Year, e.EntryDate.Month, e.EntryDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    return new Coordinate(utcDate.Ticks, ResolveY(e.EntryDate));
                },
                XToolTipLabelFormatter = point =>
                    new DateTime((long)point.SecondaryValue, DateTimeKind.Utc).ToString("MMM d, yyyy"),
                YToolTipLabelFormatter = point =>
                {
                    var entry = point.Context.DataSource as UserEntry;
                    var fgValue = (int)Math.Round(point.PrimaryValue);
                    var sentiment = SentimentClassifier.Classify(point.PrimaryValue);
                    if (entry is null) return $"F&G: {fgValue} ({sentiment})";
                    return $"{entry.Intention}  ·  ${entry.Balance:N2}  ·  F&G: {fgValue} ({sentiment})";
                },
                GeometrySize = 10,
                Stroke = new SolidColorPaint(new SKColor((byte)primaryR, (byte)primaryG, (byte)primaryB), 1.5f),
                Fill = new SolidColorPaint(new SKColor((byte)primaryR, (byte)primaryG, (byte)primaryB, 200))
            });
        }

        Series = seriesList.ToArray();

        // y axis padding — use corrected historical values for entries
        var dataValues = ordered.Select(h => h.Value).ToList();
        dataValues.AddRange(entryHistoricalValues);
        var dataMin = dataValues.Min();
        var dataMax = dataValues.Max();
        var yRange = dataMax - dataMin;
        if (yRange <= 0) yRange = 10;
        var yPaddingBelow = yRange * 0.05;
        var yPaddingAbove = yRange * 0.12; // extra room above highest
        var yMin = Math.Max(0, dataMin - yPaddingBelow);
        var yMax = Math.Min(100, dataMax + yPaddingAbove);

        XAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d, yyyy"))
            {
                MinLimit = xMin,
                MaxLimit = xMax,
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(new SKColor(0x6B, 0x69, 0x80)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0x22, 0x22, 0x38, 80)),
                TextSize = 10
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = yMin,
                MaxLimit = yMax,
                LabelsPaint = new SolidColorPaint(new SKColor(0x6B, 0x69, 0x80)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0x22, 0x22, 0x38, 80)),
                TextSize = 10
            }
        };
    }

    [RelayCommand]
    private async Task SaveEntryAsync()
    {
        BalanceError = string.Empty;

        if (string.IsNullOrWhiteSpace(BalanceText) || !decimal.TryParse(BalanceText, out var balance) || balance <= 0)
        {
            BalanceError = "Enter a valid balance amount";
            return;
        }

        try
        {
            IsBusy = true;

            // Normalize to UTC midnight to align with historical data timestamps
            var entryDateUtc = new DateTime(EntryDate.Year, EntryDate.Month, EntryDate.Day, 0, 0, 0, DateTimeKind.Utc);

            // Look up the Fear & Greed value for the selected date from cached historical data
            double fearGreedValue = 0;
            if (_cachedHistorical.Count > 0)
            {
                var match = _cachedHistorical.MinBy(h => Math.Abs((h.Timestamp.Date - entryDateUtc.Date).TotalDays));
                fearGreedValue = match?.Value ?? 0;
            }
            else
            {
                var latest = await _fearGreedService.GetLatestAsync();
                fearGreedValue = latest?.Value ?? 0;
            }

            var entry = new UserEntry
            {
                Balance = balance,
                Intention = SelectedIntention,
                EntryDate = entryDateUtc,
                FearGreedValue = fearGreedValue
            };

            await _dataService.SaveEntryAsync(entry);

            SaveSucceeded?.Invoke(this, EventArgs.Empty);

            BalanceText = string.Empty;
            SelectedIntention = "Hold";
            EntryDate = DateTime.Today;

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving entry: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(UserEntry entry)
    {
        try
        {
            await _dataService.DeleteEntryAsync(entry);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting entry: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SelectEntry(UserEntry entry)
    {
        SelectedEntry = entry;
    }

    [RelayCommand]
    private void DismissDetail()
    {
        SelectedEntry = null;
    }

    [RelayCommand]
    private void ToggleDatePicker()
    {
        IsDatePickerExpanded = !IsDatePickerExpanded;
    }

    [RelayCommand]
    private async Task SelectChartRangeAsync(string range)
    {
        if (string.IsNullOrEmpty(range) || SelectedChartRange == range) return;
        SelectedChartRange = range;
        await LoadDataAsync();
    }

    public async Task OnAppearingAsync()
    {
        await LoadDataAsync();
    }

    partial void OnYesterdayValueChanged(double value)
    {
        OnPropertyChanged(nameof(YesterdayValueWhole));
        OnPropertyChanged(nameof(YesterdayValueDecimal));
    }

    partial void OnLastWeekValueChanged(double value)
    {
        OnPropertyChanged(nameof(LastWeekValueWhole));
        OnPropertyChanged(nameof(LastWeekValueDecimal));
    }

    partial void OnLastMonthValueChanged(double value)
    {
        OnPropertyChanged(nameof(LastMonthValueWhole));
        OnPropertyChanged(nameof(LastMonthValueDecimal));
    }

    private static string GetWholePart(double value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var absFormatted = Math.Abs(rounded).ToString("0.0", CultureInfo.InvariantCulture);
        var whole = absFormatted.Split('.')[0];
        return rounded < 0 ? "-" + whole : whole;
    }

    private static string GetDecimalPart(double value)
    {
        var rounded = Math.Round(Math.Abs(value), 1, MidpointRounding.AwayFromZero);
        var formatted = rounded.ToString("0.0", CultureInfo.InvariantCulture);
        return "." + formatted.Split('.')[1];
    }
}
