using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WisprClone.Core;
using WisprClone.Infrastructure;
using WisprClone.Services;
using WisprClone.Services.Interfaces;
using WisprClone.Services.Speech;
using WisprClone.ViewModels;
using WisprClone.Views;

namespace WisprClone;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    private MainViewModel _mainViewModel = null!;
    private SystemTrayManager? _trayManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize main view model
        _mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        try
        {
            await _mainViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize application: {ex.Message}",
                "WisprClone Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Setup system tray
        _trayManager = _serviceProvider.GetRequiredService<SystemTrayManager>();
        _trayManager.Initialize();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Settings
        services.AddSingleton<ISettingsService, SettingsService>();

        // Speech recognition services - register all providers
        services.AddSingleton<OfflineSpeechRecognitionService>();
        services.AddSingleton<AzureSpeechRecognitionService>();
        services.AddSingleton<OpenAIWhisperSpeechRecognitionService>();
        services.AddSingleton<HybridSpeechRecognitionService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            var offline = sp.GetRequiredService<OfflineSpeechRecognitionService>();
            var azure = sp.GetRequiredService<AzureSpeechRecognitionService>();
            return new HybridSpeechRecognitionService(offline, azure, settings);
        });

        // Use SpeechServiceManager for runtime provider switching
        services.AddSingleton<ISpeechRecognitionService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            Log($"Initial speech provider: {settings.Current.SpeechProvider}");

            return new SpeechServiceManager(
                sp.GetRequiredService<OfflineSpeechRecognitionService>(),
                sp.GetRequiredService<AzureSpeechRecognitionService>(),
                sp.GetRequiredService<OpenAIWhisperSpeechRecognitionService>(),
                sp.GetRequiredService<HybridSpeechRecognitionService>(),
                settings);
        });

        // Other services
        services.AddSingleton<IClipboardService, ClipboardService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<OverlayViewModel>();
        services.AddSingleton<SystemTrayViewModel>();

        // Infrastructure
        services.AddSingleton<SystemTrayManager>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        (_mainViewModel as IDisposable)?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }

    private static void Log(string message)
    {
        var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [App] {message}";
        try { System.IO.File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }
}
