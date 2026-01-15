using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using WisprClone.Core;
using WisprClone.Infrastructure;
using WisprClone.Services.Interfaces;
using WisprClone.ViewModels.Base;
using WisprClone.Views;

namespace WisprClone.ViewModels;

/// <summary>
/// Main application view model that orchestrates all services and UI.
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISpeechRecognitionService _speechService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly DoubleKeyTapDetector _hotkeyDetector;

    private OverlayWindow? _overlayWindow;
    private OverlayViewModel? _overlayViewModel;
    private SettingsWindow? _settingsWindow;
    private CancellationTokenSource? _recognitionCts;
    private TranscriptionState _currentState = TranscriptionState.Idle;
    private readonly object _stateLock = new();
    private bool _disposed;

    /// <summary>
    /// Raised when the transcription state changes.
    /// </summary>
    public event EventHandler<TranscriptionState>? StateChanged;

    [ObservableProperty]
    private bool _isTranscribing;

    public MainViewModel(
        ISpeechRecognitionService speechService,
        IClipboardService clipboardService,
        ISettingsService settingsService)
    {
        _speechService = speechService;
        _clipboardService = clipboardService;
        _settingsService = settingsService;

        var settings = settingsService.Current;
        _hotkeyDetector = new DoubleKeyTapDetector(
            Key.LeftCtrl,
            settings.DoubleTapIntervalMs,
            settings.MaxKeyHoldDurationMs);

        _hotkeyDetector.DoubleTapDetected += OnDoubleTapDetected;
        _speechService.RecognitionCompleted += OnRecognitionCompleted;
        _speechService.RecognitionPartial += OnRecognitionPartial;
    }

    /// <summary>
    /// Initializes the application.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Load settings
        await _settingsService.LoadAsync();

        // Initialize speech recognition
        await _speechService.InitializeAsync(_settingsService.Current.RecognitionLanguage);

        // Start hotkey detection
        _hotkeyDetector.Start();

        // Create overlay window
        _overlayViewModel = new OverlayViewModel(_speechService, _settingsService);
        _overlayWindow = new OverlayWindow
        {
            DataContext = _overlayViewModel
        };

        // Show overlay if not set to start minimized
        if (!_settingsService.Current.StartMinimized)
        {
            _overlayWindow.Show();
            _overlayViewModel.Show();
        }
    }

    private async void OnDoubleTapDetected(object? sender, EventArgs e)
    {
        Log($"DoubleTap detected, current state: {_currentState}");

        // Toggle transcription
        if (_currentState == TranscriptionState.Idle)
        {
            Log("Starting transcription...");
            await StartTranscriptionAsync();
            Log("StartTranscriptionAsync completed");
        }
        else if (_currentState == TranscriptionState.Listening)
        {
            Log("Stopping transcription...");
            await StopTranscriptionAsync();
            Log("StopTranscriptionAsync completed");
        }
    }

    private void Log(string message)
    {
        // Only log if logging is enabled in settings
        if (!_settingsService.Current.EnableLogging)
            return;

        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logsDir = System.IO.Path.Combine(appData, Constants.AppName, "logs");
            System.IO.Directory.CreateDirectory(logsDir);

            var logFileName = $"wispr_{DateTime.Now:yyyy-MM-dd}.log";
            var logPath = System.IO.Path.Combine(logsDir, logFileName);

            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            System.IO.File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch
        {
            // Silently ignore logging errors
        }
    }

    private async Task StartTranscriptionAsync()
    {
        if (!TryTransitionState(TranscriptionState.Idle, TranscriptionState.Initializing))
            return;

        _recognitionCts = new CancellationTokenSource();

        // Set timeout for maximum recording duration
        var maxDurationSeconds = _settingsService.Current.MaxRecordingDurationSeconds;
        if (maxDurationSeconds > 0)
        {
            _recognitionCts.CancelAfter(TimeSpan.FromSeconds(maxDurationSeconds));
            Log($"Recording timeout set to {maxDurationSeconds} seconds");
        }

        try
        {
            // Show overlay
            ShowOverlay();

            // Transition to listening
            SetState(TranscriptionState.Listening);
            IsTranscribing = true;

            await _speechService.StartRecognitionAsync(_recognitionCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout reached - gracefully stop recording
            Log("Recording stopped due to maximum duration timeout");
            await HandleRecordingTimeoutAsync();
        }
        catch (Exception)
        {
            SetState(TranscriptionState.Error);
            // Small delay before going back to idle
            await Task.Delay(1000);
            SetState(TranscriptionState.Idle);
            IsTranscribing = false;
        }
    }

    private async Task HandleRecordingTimeoutAsync()
    {
        // Transition to processing state
        SetState(TranscriptionState.Processing);

        try
        {
            var finalText = await _speechService.StopRecognitionAsync();
            Log($"Timeout: StopRecognitionAsync returned, text length: {finalText?.Length ?? 0}");

            // Copy to clipboard if enabled
            if (_settingsService.Current.AutoCopyToClipboard && !string.IsNullOrWhiteSpace(finalText))
            {
                _ = _clipboardService.SetTextAsync(finalText);
            }

            SetState(TranscriptionState.Idle);
            IsTranscribing = false;

            // Hide overlay after delay
            _ = HideOverlayAfterDelayAsync(3000);
        }
        catch (Exception ex)
        {
            Log($"Timeout handling error: {ex.Message}");
            SetState(TranscriptionState.Error);
            await Task.Delay(1000);
            SetState(TranscriptionState.Idle);
            IsTranscribing = false;
        }
        finally
        {
            _recognitionCts?.Dispose();
            _recognitionCts = null;
        }
    }

    private async Task StopTranscriptionAsync()
    {
        if (!TryTransitionState(TranscriptionState.Listening, TranscriptionState.Processing))
            return;

        try
        {
            var finalText = await _speechService.StopRecognitionAsync();
            Log($"StopRecognitionAsync returned, text length: {finalText?.Length ?? 0}");

            // Copy to clipboard if enabled (fire-and-forget, don't block)
            // Real-time copy already happens in OnRecognitionPartial
            if (_settingsService.Current.AutoCopyToClipboard && !string.IsNullOrWhiteSpace(finalText))
            {
                _ = _clipboardService.SetTextAsync(finalText);
            }

            SetState(TranscriptionState.Idle);
            IsTranscribing = false;
            Log("State set to Idle");

            // Hide overlay after 3 seconds
            _ = HideOverlayAfterDelayAsync(3000);
        }
        catch (Exception ex)
        {
            Log($"StopTranscriptionAsync error: {ex.Message}");
            SetState(TranscriptionState.Error);
            await Task.Delay(1000);
            SetState(TranscriptionState.Idle);
            IsTranscribing = false;
        }
        finally
        {
            _recognitionCts?.Dispose();
            _recognitionCts = null;
        }
    }

    private async Task HideOverlayAfterDelayAsync(int delayMs)
    {
        await Task.Delay(delayMs);

        // Only hide if we're still idle (not started a new session)
        // and user is not interacting with the overlay
        if (_currentState == TranscriptionState.Idle)
        {
            // Check if user is interacting (mouse over or dropdown open)
            if (_overlayViewModel?.IsUserInteracting == true)
            {
                Log("Overlay hide cancelled - user is interacting");
                // Retry hiding after user stops interacting
                _ = RetryHideOverlayAsync();
                return;
            }

            HideOverlay();
            Log("Overlay hidden after delay");
        }
    }

    private async Task RetryHideOverlayAsync()
    {
        // Wait for user to stop interacting, then hide after a short delay
        while (_overlayViewModel?.IsUserInteracting == true && _currentState == TranscriptionState.Idle)
        {
            await Task.Delay(500);
        }

        // Wait a bit more after interaction ends
        await Task.Delay(2000);

        // Final check before hiding
        if (_currentState == TranscriptionState.Idle && _overlayViewModel?.IsUserInteracting != true)
        {
            HideOverlay();
            Log("Overlay hidden after user stopped interacting");
        }
    }

    private async void OnRecognitionPartial(object? sender, TranscriptionEventArgs e)
    {
        // Copy to clipboard in real-time as text is being dictated
        if (_settingsService.Current.AutoCopyToClipboard && !string.IsNullOrWhiteSpace(e.Text))
        {
            await _clipboardService.SetTextAsync(e.Text);
        }
    }

    private async void OnRecognitionCompleted(object? sender, TranscriptionEventArgs e)
    {
        // Final copy when recognition completes
        if (e.IsFinal && _settingsService.Current.AutoCopyToClipboard && !string.IsNullOrWhiteSpace(e.Text))
        {
            await _clipboardService.SetTextAsync(e.Text);
        }
    }

    private bool TryTransitionState(TranscriptionState expectedCurrent, TranscriptionState newState)
    {
        lock (_stateLock)
        {
            if (_currentState != expectedCurrent)
                return false;

            _currentState = newState;
            StateChanged?.Invoke(this, newState);
            return true;
        }
    }

    private void SetState(TranscriptionState newState)
    {
        lock (_stateLock)
        {
            _currentState = newState;
            StateChanged?.Invoke(this, newState);
        }
    }

    /// <summary>
    /// Shows the overlay window.
    /// </summary>
    public void ShowOverlay()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.Show();
            _overlayWindow?.Activate();
            _overlayViewModel?.Show();
        });
    }

    /// <summary>
    /// Hides the overlay window.
    /// </summary>
    public void HideOverlay()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _overlayViewModel?.SavePosition();
            _overlayWindow?.Hide();
            _overlayViewModel?.Hide();
        });
    }

    /// <summary>
    /// Toggles the overlay window visibility.
    /// </summary>
    public void ToggleOverlay()
    {
        if (_overlayWindow?.IsVisible == true)
        {
            HideOverlay();
        }
        else
        {
            ShowOverlay();
        }
    }

    /// <summary>
    /// Opens the settings window.
    /// </summary>
    public void OpenSettings()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(_settingsService);
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hotkeyDetector.DoubleTapDetected -= OnDoubleTapDetected;
        _speechService.RecognitionCompleted -= OnRecognitionCompleted;
        _speechService.RecognitionPartial -= OnRecognitionPartial;

        // Stop transcription if active
        if (_currentState == TranscriptionState.Listening)
        {
            _recognitionCts?.Cancel();
        }

        _hotkeyDetector.Dispose();
        _speechService.Dispose();
        _recognitionCts?.Dispose();

        Application.Current?.Dispatcher.Invoke(() =>
        {
            _overlayViewModel?.SavePosition();
            _overlayWindow?.Close();
            _settingsWindow?.Close();
        });

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~MainViewModel()
    {
        Dispose();
    }
}
