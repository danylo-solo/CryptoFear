using CryptoFear.Helpers;
using CryptoFear.ViewModels;

namespace CryptoFear.Views;

public partial class NewsPage : ContentPage
{
    private readonly NewsViewModel _viewModel;

    public NewsPage(NewsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        await ArticleList.FadeInFromBottomAsync(350, 20);
    }
}
