using CryptoFear.Helpers;
using CryptoFear.ViewModels;

namespace CryptoFear.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private IDispatcherTimer? _headlineTimer;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        await AnimateCardsAsync();
        UpdateChipStyles();

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.SelectedTimeframe))
                UpdateChipStyles();
        };

        StartHeadlineRotation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopHeadlineRotation();
    }

    private void StartHeadlineRotation()
    {
        if (!_viewModel.HasHeadlines) return;

        StopHeadlineRotation();

        _headlineTimer = Dispatcher.CreateTimer();
        _headlineTimer.Interval = TimeSpan.FromSeconds(5);
        _headlineTimer.Tick += async (s, e) => await RotateHeadlineAsync();
        _headlineTimer.Start();
    }

    private void StopHeadlineRotation()
    {
        _headlineTimer?.Stop();
        _headlineTimer = null;
    }

    private async Task RotateHeadlineAsync()
    {
        try
        {
            await HeadlineContent.FadeTo(0, 200, Easing.CubicIn);
            _viewModel.AdvanceHeadline();
            await HeadlineContent.FadeTo(1, 200, Easing.CubicOut);
        }
        catch
        {
            HeadlineContent.Opacity = 1;
        }
    }

    private async Task AnimateCardsAsync()
    {
        await NewsBanner.FadeInFromBottomAsync(300, 15);

        var statCards = new View[] { StatCard1, StatCard2, StatCard3, YearlyHighCard, YearlyLowCard };
        await statCards.FadeInStaggeredAsync(80, 350);

        await Task.Delay(100);

        var actionCards = new View[] { LogActionCard, WatchActionCard };
        await actionCards.FadeInStaggeredAsync(60, 300);
    }

    private void UpdateChipStyles()
    {
        var chips = new Dictionary<string, Button>
        {
            { "1H", Chip1H },
            { "24H", Chip24H },
            { "7D", Chip7D },
            { "30D", Chip30D }
        };

        var selectedStyle = Application.Current!.Resources["ChipButtonSelected"] as Style;
        var normalStyle = Application.Current!.Resources["ChipButton"] as Style;

        foreach (var (timeframe, button) in chips)
        {
            button.Style = timeframe == _viewModel.SelectedTimeframe ? selectedStyle : normalStyle;
        }
    }

    private async void OnLogEntryTapped(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            ViewAnimations.PerformHapticFeedback();
            await view.PressAnimationAsync();
        }
        await Shell.Current.GoToAsync("//chart");
    }

    private async void OnWatchWalletTapped(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            ViewAnimations.PerformHapticFeedback();
            await view.PressAnimationAsync();
        }
        await Shell.Current.GoToAsync("//watch");
    }

    private async void OnNewsTapped(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            ViewAnimations.PerformHapticFeedback();
            await view.PressAnimationAsync();
        }
        await Shell.Current.GoToAsync("//news");
    }
}
