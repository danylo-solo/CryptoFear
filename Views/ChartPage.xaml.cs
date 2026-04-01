using CryptoFear.Helpers;
using CryptoFear.Services;
using CryptoFear.ViewModels;
using LiveChartsCore.Kernel;

namespace CryptoFear.Views;

public partial class ChartPage : ContentPage
{
    private readonly ChartViewModel _viewModel;

    public ChartPage(ChartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        ChartCard.DataPointerDown += OnChartDataPointerDown;
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

    private void OnChartDataPointerDown(
        LiveChartsCore.Kernel.Sketches.IChartView chart,
        IEnumerable<ChartPoint> points)
    {
        var entryPoint = points.FirstOrDefault(p => p.Context.DataSource is Models.UserEntry);

        if (entryPoint?.Context.DataSource is Models.UserEntry entry)
        {
            ViewAnimations.PerformHapticFeedback();
            _viewModel.ShowMarkerTooltip(entry);
        }
        else
        {
            _viewModel.DismissMarkerTooltipCommand.Execute(null);
        }
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

    private async void OnDeleteEntryClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton button) return;
        if (button.BindingContext is not Models.UserEntry entry) return;

        var confirmed = await DisplayAlert(
            "Delete Entry",
            $"Are you sure you want to permanently delete the entry from {entry.EntryDate:MMM dd, yyyy}?",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            ViewAnimations.PerformHapticFeedback(HapticFeedbackType.LongPress);
            _viewModel.DeleteEntryCommand.Execute(entry);
        }
    }
}
