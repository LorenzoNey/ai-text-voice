using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Resources;
using WisprClone.Core;
using WisprClone.ViewModels;

namespace WisprClone.Infrastructure;

/// <summary>
/// Manages the system tray icon and context menu using Windows Forms NotifyIcon.
/// </summary>
public class SystemTrayManager : IDisposable
{
    private readonly MainViewModel _mainViewModel;
    private readonly SystemTrayViewModel _trayViewModel;
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    // Cached icons for different states
    private Icon? _iconIdle;
    private Icon? _iconListening;
    private Icon? _iconProcessing;
    private Icon? _iconError;

    public SystemTrayManager(MainViewModel mainViewModel, SystemTrayViewModel trayViewModel)
    {
        _mainViewModel = mainViewModel;
        _trayViewModel = trayViewModel;
    }

    /// <summary>
    /// Initializes the system tray icon.
    /// </summary>
    public void Initialize()
    {
        try
        {
            // Load icons from resources
            LoadIcons();

            _notifyIcon = new NotifyIcon
            {
                Text = "WisprClone - Ready (Ctrl+Ctrl to start)",
                Icon = _iconIdle ?? CreateFallbackIcon(Color.Gray),
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show Overlay");
            showItem.Click += (s, e) => Application.Current?.Dispatcher.Invoke(() => _mainViewModel.ShowOverlay());
            contextMenu.Items.Add(showItem);

            var hideItem = new ToolStripMenuItem("Hide Overlay");
            hideItem.Click += (s, e) => Application.Current?.Dispatcher.Invoke(() => _mainViewModel.HideOverlay());
            contextMenu.Items.Add(hideItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (s, e) => Application.Current?.Dispatcher.Invoke(() => _mainViewModel.OpenSettings());
            contextMenu.Items.Add(settingsItem);

            var aboutItem = new ToolStripMenuItem("About...");
            aboutItem.Click += (s, e) => Application.Current?.Dispatcher.Invoke(() => ShowAboutDialog());
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Double-click to toggle overlay
            _notifyIcon.DoubleClick += (s, e) => Application.Current?.Dispatcher.Invoke(() => _mainViewModel.ToggleOverlay());

            // Subscribe to state changes
            _mainViewModel.StateChanged += OnStateChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize tray icon: {ex.Message}");
            System.Windows.MessageBox.Show($"System tray icon failed to initialize: {ex.Message}",
                "WisprClone Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnStateChanged(object? sender, TranscriptionState state)
    {
        if (_notifyIcon == null) return;

        try
        {
            _notifyIcon.Text = state switch
            {
                TranscriptionState.Idle => "WisprClone - Ready (Ctrl+Ctrl)",
                TranscriptionState.Initializing => "WisprClone - Initializing...",
                TranscriptionState.Listening => "WisprClone - Listening...",
                TranscriptionState.Processing => "WisprClone - Processing...",
                TranscriptionState.CopyingToClipboard => "WisprClone - Copied!",
                TranscriptionState.Error => "WisprClone - Error",
                _ => "WisprClone"
            };

            // Update icon based on state (use cached icons)
            _notifyIcon.Icon = state switch
            {
                TranscriptionState.Listening => _iconListening ?? CreateFallbackIcon(Color.LimeGreen),
                TranscriptionState.Processing => _iconProcessing ?? CreateFallbackIcon(Color.Orange),
                TranscriptionState.Error => _iconError ?? CreateFallbackIcon(Color.Red),
                _ => _iconIdle ?? CreateFallbackIcon(Color.Gray)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update tray icon: {ex.Message}");
        }
    }

    private static void ShowAboutDialog()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";

        System.Windows.MessageBox.Show(
            $"{Constants.AppName}\n\n" +
            $"Version: {versionString}\n\n" +
            "A voice-to-text transcription tool for Windows.\n\n" +
            "Double-tap Ctrl to start/stop dictation.",
            $"About {Constants.AppName}",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void LoadIcons()
    {
        try
        {
            _iconIdle = LoadIconFromResource("pack://application:,,,/Resources/Icons/tray_idle.ico");
            _iconListening = LoadIconFromResource("pack://application:,,,/Resources/Icons/tray_listening.ico");
            _iconProcessing = LoadIconFromResource("pack://application:,,,/Resources/Icons/tray_processing.ico");
            _iconError = LoadIconFromResource("pack://application:,,,/Resources/Icons/tray_error.ico");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load icons from resources: {ex.Message}");
            // Icons will remain null, and fallback icons will be used
        }
    }

    private static Icon? LoadIconFromResource(string resourcePath)
    {
        try
        {
            var uri = new Uri(resourcePath, UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(uri);
            if (streamInfo != null)
            {
                return new Icon(streamInfo.Stream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load icon {resourcePath}: {ex.Message}");
        }
        return null;
    }

    private static Icon CreateFallbackIcon(Color color)
    {
        const int size = 16; // Standard system tray icon size
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 1.5f);

        // Simple microphone shape for 16x16
        // Microphone head
        graphics.FillEllipse(brush, 4, 1, 8, 9);

        // Microphone stand arc
        graphics.DrawArc(pen, 3, 6, 10, 6, 0, 180);

        // Microphone base
        graphics.DrawLine(pen, 8, 12, 8, 14);
        graphics.DrawLine(pen, 4, 14, 12, 14);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _mainViewModel.StateChanged -= OnStateChanged;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        // Dispose cached icons
        _iconIdle?.Dispose();
        _iconListening?.Dispose();
        _iconProcessing?.Dispose();
        _iconError?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SystemTrayManager()
    {
        Dispose();
    }
}
