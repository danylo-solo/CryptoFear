using CryptoFear.Helpers;
using CryptoFear.ViewModels;

namespace CryptoFear.Views;

public partial class WatchPage : ContentPage
{
    private readonly WatchViewModel _viewModel;

    public WatchPage(WatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        UpdateChipStyles();

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(WatchViewModel.SelectedTimeframe))
                UpdateChipStyles();
        };

        await FormCard.FadeInFromBottomAsync(350, 20);
    }

    private void UpdateChipStyles()
    {
        var chips = new Dictionary<string, Button>
        {
            { "1H", Chip1H },
            { "24H", Chip24H },
            { "7D", Chip7D },
            { "30D", Chip30D },
            { "1Y", Chip1Y }
        };

        var selectedStyle = Application.Current!.Resources["ChipButtonSelected"] as Style;
        var normalStyle = Application.Current!.Resources["ChipButton"] as Style;

        foreach (var (timeframe, button) in chips)
        {
            button.Style = timeframe == _viewModel.SelectedTimeframe ? selectedStyle : normalStyle;
        }
    }

    private void OnDeleteSwipeItemInvoked(object? sender, EventArgs e)
    {
        ViewAnimations.PerformHapticFeedback(HapticFeedbackType.LongPress);

        if (sender is SwipeItem swipeItem)
        {
            var parent = swipeItem.Parent;
            SwipeView? swipeView = null;

            while (parent != null)
            {
                if (parent is SwipeView sv)
                {
                    swipeView = sv;
                    break;
                }
                parent = parent.Parent;
            }

            if (swipeView?.BindingContext is Models.WatchedWallet wallet)
            {
                _viewModel.DeleteWalletCommand.Execute(wallet);
            }
        }
    }
}
