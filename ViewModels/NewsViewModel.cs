using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoFear.Models;
using CryptoFear.Services;

namespace CryptoFear.ViewModels;

public partial class NewsViewModel : BaseViewModel
{
    private readonly INewsService _newsService;
    private List<NewsArticle> _allArticles = new();

    private const int PageSize = 10;

    [ObservableProperty]
    private ObservableCollection<NewsArticle> articles = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    [ObservableProperty]
    private bool canGoBack;

    [ObservableProperty]
    private bool canGoForward;

    public NewsViewModel(INewsService newsService)
    {
        _newsService = newsService;
        Title = "News";
    }

    [RelayCommand]
    private async Task LoadArticlesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            _allArticles = await _newsService.GetArticlesAsync();
            CurrentPage = 1;
            UpdatePage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading articles: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshArticlesAsync()
    {
        try
        {
            _allArticles = await _newsService.GetArticlesAsync(forceRefresh: true);
            CurrentPage = 1;
            UpdatePage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing articles: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePage();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling(_allArticles.Count / (double)PageSize));

        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        var pageItems = _allArticles
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Articles = new ObservableCollection<NewsArticle>(pageItems);
        CanGoBack = CurrentPage > 1;
        CanGoForward = CurrentPage < TotalPages;
    }

    [RelayCommand]
    private async Task OpenArticle(NewsArticle article)
    {
        if (string.IsNullOrWhiteSpace(article.Url))
            return;

        try
        {
            await Browser.Default.OpenAsync(article.Url, BrowserLaunchMode.External);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening URL: {ex.Message}");
        }
    }

    public async Task OnAppearingAsync()
    {
        await LoadArticlesAsync();
    }
}
