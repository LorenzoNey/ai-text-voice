using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WisprClone.Infrastructure;
using WisprClone.Infrastructure.Keyboard;
using WisprClone.Services;
using WisprClone.Services.Interfaces;
using WisprClone.Services.Speech;
using WisprClone.ViewModels;

namespace WisprClone;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    private MainViewModel _mainViewModel = null!;
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
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
            Log($"Failed to initialize: {ex.Message}");
            // TODO: Show error dialog
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(1);
            }
            return;
        }

        // Setup system tray
        SetupTrayIcon();

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Settings (cross-platform)
        services.AddSingleton<ISettingsService, SettingsService>();

        // Clipboard service (cross-platform via Avalonia)
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();

        // Global keyboard hook (cross-platform via SharpHook)
        services.AddSingleton<IGlobalKeyboardHook, SharpHookKeyboardHook>();

        // Platform-specific speech services
        if (OperatingSystem.IsWindows())
        {
            // Windows: All providers available
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
        }
        else
        {
            // macOS/Linux: Cloud providers only (Azure + OpenAI)
            services.AddSingleton<AzureSpeechRecognitionService>();
            services.AddSingleton<OpenAIWhisperSpeechRecognitionService>();

            services.AddSingleton<ISpeechRecognitionService>(sp =>
            {
                var settings = sp.GetRequiredService<ISettingsService>();
                Log($"Initial speech provider (non-Windows): {settings.Current.SpeechProvider}");

                // Use SpeechServiceManager without offline services
                return new CrossPlatformSpeechServiceManager(
                    sp.GetRequiredService<AzureSpeechRecognitionService>(),
                    sp.GetRequiredService<OpenAIWhisperSpeechRecognitionService>(),
                    settings);
            });
        }

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<OverlayViewModel>();
        services.AddSingleton<SystemTrayViewModel>();
    }

    private void SetupTrayIcon()
    {
        var trayViewModel = _serviceProvider.GetRequiredService<SystemTrayViewModel>();

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(GetIconStream("tray_idle.ico")),
            ToolTipText = "WisprClone - Ready (Ctrl+Ctrl to start)",
            Menu = CreateTrayMenu(trayViewModel)
        };

        _trayIcon.Clicked += (_, _) => trayViewModel.ToggleOverlayCommand.Execute(null);

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private NativeMenu CreateTrayMenu(SystemTrayViewModel viewModel)
    {
        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show Overlay");
        showItem.Click += (_, _) => viewModel.ShowOverlayCommand.Execute(null);
        menu.Items.Add(showItem);

        var hideItem = new NativeMenuItem("Hide Overlay");
        hideItem.Click += (_, _) => viewModel.HideOverlayCommand.Execute(null);
        menu.Items.Add(hideItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var settingsItem = new NativeMenuItem("Settings...");
        settingsItem.Click += (_, _) => viewModel.OpenSettingsCommand.Execute(null);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };
        menu.Items.Add(exitItem);

        return menu;
    }

    private Stream GetIconStream(string iconName)
    {
        // Try to load from resources
        var assembly = typeof(App).Assembly;
        var resourceName = $"WisprClone.Resources.Icons.{iconName}";
        var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // Fallback: try avares
            var uri = new Uri($"avares://WisprClone/Resources/Icons/{iconName}");
            return AssetLoader.Open(uri);
        }

        return stream;
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [App] {message}";
        try { File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }
}
