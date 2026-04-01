using CryptoFear.Services;
using Microsoft.Maui.Devices;

namespace CryptoFear;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        EnvConfig.Load();
        ThemeService.Initialize();
        CurrencyService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Force mobile resolution on Windows for desktop preview
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            const int mobileWidth = 390;   // iPhone 14 portrait; use 360x780 for Android-style
            const int mobileHeight = 844;
            window.Width = mobileWidth;
            window.Height = mobileHeight;
            window.MinimumWidth = mobileWidth;
            window.MinimumHeight = mobileHeight;
            window.MaximumWidth = mobileWidth;
            window.MaximumHeight = mobileHeight;
        }

        Task.Run(async () =>
        {
            try
            {
                var dataService = Handler?.MauiContext?.Services.GetService<IDataService>();
                if (dataService != null)
                {
                    await dataService.InitializeDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
            }
        });

        return window;
    }
}
