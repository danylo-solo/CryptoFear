using CryptoFear.Services;
using CryptoFear.ViewModels;

namespace CryptoFear.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.IsCurrencyDropdownOpen) && viewModel.IsCurrencyDropdownOpen)
            {
                Dispatcher.Dispatch(SyncCheckMarks);
            }
        };
    }

    private void OnCurrencyOptionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.BindingContext is not CurrencyOption currency) return;

        _viewModel.SelectCurrencyCommand.Execute(currency);
    }

    private void SyncCheckMarks()
    {
        var selected = CurrencyService.Current.Code;
        VisitCheckMarks(this, (label, context) =>
        {
            if (context is CurrencyOption opt)
                label.IsVisible = opt.Code == selected;
        });
    }

    private static void VisitCheckMarks(Element parent, Action<Label, object?> action)
    {
        if (parent is Label label && label.Text == "✓")
        {
            var ctx = (parent as BindableObject)?.BindingContext
                      ?? parent.Parent?.BindingContext;
            action(label, ctx);
            return;
        }

        IEnumerable<Element>? children = parent switch
        {
            Layout layout => layout.Children.OfType<Element>(),
            ContentPage page => page.Content is Element e ? new[] { e } : Enumerable.Empty<Element>(),
            ScrollView sv => sv.Content is Element el ? new[] { el } : Enumerable.Empty<Element>(),
            Border b => b.Content is Element be ? new[] { be } : Enumerable.Empty<Element>(),
            _ => Enumerable.Empty<Element>()
        };

        foreach (var child in children)
            VisitCheckMarks(child, action);
    }
}
