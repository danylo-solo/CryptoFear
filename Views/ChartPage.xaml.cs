using CryptoFear.Helpers;
using CryptoFear.ViewModels;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CryptoFear.Views;

public partial class ChartPage : ContentPage
{
    private readonly ChartViewModel _viewModel;

    public ChartPage(ChartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        // Match tooltip colours to the app's dark theme
        ChartCard.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(0x1C, 0x1C, 0x36, 245));
        ChartCard.TooltipTextPaint = new SolidColorPaint(new SKColor(0xE8, 0xE6, 0xF0));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();

        await ChartCard.FadeInFromBottomAsync(350, 20);
        await StatsCard.FadeInFromBottomAsync(300, 20);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private async void OnIntentionSelectorTapped(object? sender, TappedEventArgs e)
    {
        var result = await DisplayActionSheet(
            "Select Intention",
            "Cancel",
            null,
            _viewModel.Intentions.ToArray());

        if (!string.IsNullOrEmpty(result) && result != "Cancel")
        {
            _viewModel.SelectedIntention = result;
        }
    }

    private void OnEntryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.BindingContext is not Models.UserEntry entry) return;

        ViewAnimations.PerformHapticFeedback();
        _viewModel.SelectEntryCommand.Execute(entry);
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

            if (swipeView?.BindingContext is Models.UserEntry entry)
            {
                _viewModel.DeleteEntryCommand.Execute(entry);
            }
        }
    }
}
