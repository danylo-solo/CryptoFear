using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using CryptoFear.Services;
using CryptoFear.ViewModels;
using CryptoFear.Views;

namespace CryptoFear;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                Microsoft.Maui.Handlers.SwitchHandler.Mapper.AppendToMapping("CompactToggle", (handler, view) =>
                {
                    if (handler.PlatformView is Microsoft.UI.Xaml.Controls.ToggleSwitch toggle)
                    {
                        toggle.OnContent = null;
                        toggle.OffContent = null;
                        toggle.MinWidth = 0;
                        toggle.Padding = new Microsoft.UI.Xaml.Thickness(0);
                        toggle.Width = 50;
                        toggle.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right;
                    }
                });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Inter-Light.ttf", "InterLight");
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        // HttpClient
        builder.Services.AddSingleton<HttpClient>();

        // Services
        builder.Services.AddSingleton<IDataService, DataService>();
        builder.Services.AddSingleton<FearGreedService>();
        builder.Services.AddSingleton<RawMarketDataService>();
        builder.Services.AddSingleton<SentimentCalculator>();
        builder.Services.AddSingleton<IFearGreedService, LocalFearGreedService>();
        builder.Services.AddSingleton<WalletService>();
        builder.Services.AddSingleton<INewsService, NewsService>();

        // ViewModels
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<NewsViewModel>();
        builder.Services.AddSingleton<ChartViewModel>();
        builder.Services.AddSingleton<WatchViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Pages
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<NewsPage>();
        builder.Services.AddSingleton<ChartPage>();
        builder.Services.AddSingleton<WatchPage>();
        builder.Services.AddSingleton<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
