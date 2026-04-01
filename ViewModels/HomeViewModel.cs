using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoFear.Models;
using CryptoFear.Services;
using System.Globalization;

namespace CryptoFear.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IFearGreedService _fearGreedService;
    private readonly INewsService _newsService;

    private List<NewsArticle> _recentHeadlines = new();
    private int _headlineIndex;

    [ObservableProperty]
    private string currentHeadlineTitle = "Recent News";

    [ObservableProperty]
    private string currentHeadlineSource = "See what's moving the market";

    [ObservableProperty]
    private bool hasHeadlines;

    [ObservableProperty]
    private double currentIndex;

    [ObservableProperty]
    private string currentClassification = string.Empty;

    [ObservableProperty]
    private double yesterdayIndex;

    [ObservableProperty]
    private double weekAgoIndex;

    [ObservableProperty]
    private double monthAgoIndex;

    [ObservableProperty]
    private double yearlyHigh;

    [ObservableProperty]
    private string yearlyHighDate = "";

    [ObservableProperty]
    private double yearlyLow;

    [ObservableProperty]
    private string yearlyLowDate = string.Empty;

    [ObservableProperty]
    private string lastUpdated = "";

    [ObservableProperty]
    private string selectedTimeframe = "24H";

    [ObservableProperty]
    private double indexChange;

    [ObservableProperty]
    private string indexChangeText = "";

    [ObservableProperty]
    private bool isPositiveChange;

    private List<FearGreedEntry> _historicalData = new();

    public HomeViewModel(IFearGreedService fearGreedService, INewsService newsService)
    {
        _fearGreedService = fearGreedService;
        _newsService = newsService;
        Title = "Home";
    }

    public string CurrentIndexWhole => GetWholePart(CurrentIndex);
    public string CurrentIndexDecimal => GetDecimalPart(CurrentIndex);
    public string YesterdayIndexWhole => GetWholePart(YesterdayIndex);
    public string YesterdayIndexDecimal => GetDecimalPart(YesterdayIndex);
    public string WeekAgoIndexWhole => GetWholePart(WeekAgoIndex);
    public string WeekAgoIndexDecimal => GetDecimalPart(WeekAgoIndex);
    public string YearlyHighWhole => GetWholePart(YearlyHigh);
    public string YearlyHighDecimal => GetDecimalPart(YearlyHigh);
    public string YearlyLowWhole => GetWholePart(YearlyLow);
    public string YearlyLowDecimal => GetDecimalPart(YearlyLow);
    public string IndexChangeWhole => GetWholePart(IndexChange, includeSign: true);
    public string IndexChangeDecimal => GetDecimalPart(IndexChange);

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            _historicalData = await _fearGreedService.GetHistoricalAsync(365);

            if (_historicalData.Count > 0)
            {
                var latest = _historicalData[0];
                CurrentIndex = latest.Value;
                CurrentClassification = latest.Classification;
                LastUpdated = FormatTimeAgo(latest.Timestamp);

                if (_historicalData.Count > 1)
                    YesterdayIndex = _historicalData[1].Value;

                if (_historicalData.Count > 7)
                    WeekAgoIndex = _historicalData[7].Value;

                if (_historicalData.Count > 30)
                    MonthAgoIndex = _historicalData[29].Value;

                var highEntry = _historicalData.MaxBy(h => h.Value);
                var lowEntry = _historicalData.MinBy(h => h.Value);
                if (highEntry != null)
                {
                    YearlyHigh = highEntry.Value;
                    YearlyHighDate = highEntry.Timestamp.ToString("MMM dd, yyyy");
                }
                if (lowEntry != null)
                {
                    YearlyLow = lowEntry.Value;
                    YearlyLowDate = lowEntry.Timestamp.ToString("MMM dd, yyyy");
                }

                UpdateChangeDisplay();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectTimeframe(string timeframe)
    {
        SelectedTimeframe = timeframe;
        UpdateChangeDisplay();
    }

    private void UpdateChangeDisplay()
    {
        double compareValue = SelectedTimeframe switch
        {
            "1H" => CurrentIndex,
            "24H" => YesterdayIndex,
            "7D" => WeekAgoIndex,
            "30D" => MonthAgoIndex,
            _ => YesterdayIndex
        };

        if (compareValue > 0)
        {
            IndexChange = CurrentIndex - compareValue;
            IsPositiveChange = IndexChange >= 0;
            IndexChangeText = IndexChange >= 0 ? $"+{IndexChange:F1}" : $"{IndexChange:F1}";
        }
        else
        {
            IndexChange = 0;
            IsPositiveChange = true;
            IndexChangeText = "0";
        }
    }

    private static string FormatTimeAgo(DateTime timestamp)
    {
        var diff = DateTime.UtcNow - timestamp;

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";

        return $"{(int)diff.TotalDays}d ago";
    }

    public async Task OnAppearingAsync()
    {
        await LoadDataAsync();
        await LoadHeadlinesAsync();
    }

    private async Task LoadHeadlinesAsync()
    {
        try
        {
            var articles = await _newsService.GetArticlesAsync();
            _recentHeadlines = articles.Take(3).ToList();
            _headlineIndex = 0;
            HasHeadlines = _recentHeadlines.Count > 0;

            if (HasHeadlines)
            {
                CurrentHeadlineTitle = _recentHeadlines[0].Title;
                CurrentHeadlineSource = _recentHeadlines[0].Source;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading headlines: {ex.Message}");
        }
    }

    public void AdvanceHeadline()
    {
        if (_recentHeadlines.Count == 0) return;

        _headlineIndex = (_headlineIndex + 1) % _recentHeadlines.Count;
        CurrentHeadlineTitle = _recentHeadlines[_headlineIndex].Title;
        CurrentHeadlineSource = _recentHeadlines[_headlineIndex].Source;
    }

    partial void OnCurrentIndexChanged(double value)
    {
        OnPropertyChanged(nameof(CurrentIndexWhole));
        OnPropertyChanged(nameof(CurrentIndexDecimal));
    }

    partial void OnYesterdayIndexChanged(double value)
    {
        OnPropertyChanged(nameof(YesterdayIndexWhole));
        OnPropertyChanged(nameof(YesterdayIndexDecimal));
    }

    partial void OnWeekAgoIndexChanged(double value)
    {
        OnPropertyChanged(nameof(WeekAgoIndexWhole));
        OnPropertyChanged(nameof(WeekAgoIndexDecimal));
    }

    partial void OnYearlyHighChanged(double value)
    {
        OnPropertyChanged(nameof(YearlyHighWhole));
        OnPropertyChanged(nameof(YearlyHighDecimal));
    }

    partial void OnYearlyLowChanged(double value)
    {
        OnPropertyChanged(nameof(YearlyLowWhole));
        OnPropertyChanged(nameof(YearlyLowDecimal));
    }

    partial void OnIndexChangeChanged(double value)
    {
        OnPropertyChanged(nameof(IndexChangeWhole));
        OnPropertyChanged(nameof(IndexChangeDecimal));
    }

    private static string GetWholePart(double value, bool includeSign = false)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var absFormatted = Math.Abs(rounded).ToString("0.0", CultureInfo.InvariantCulture);
        var whole = absFormatted.Split('.')[0];

        if (!includeSign)
            return rounded < 0 ? "-" + whole : whole;

        var sign = rounded >= 0 ? "+" : "-";
        return sign + whole;
    }

    private static string GetDecimalPart(double value)
    {
        var rounded = Math.Round(Math.Abs(value), 1, MidpointRounding.AwayFromZero);
        var formatted = rounded.ToString("0.0", CultureInfo.InvariantCulture);
        return "." + formatted.Split('.')[1];
    }
}
