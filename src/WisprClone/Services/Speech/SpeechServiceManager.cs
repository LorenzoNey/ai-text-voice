using WisprClone.Core;
using WisprClone.Services.Interfaces;

namespace WisprClone.Services.Speech;

/// <summary>
/// Manages speech recognition providers and allows switching at runtime.
/// </summary>
public class SpeechServiceManager : ISpeechRecognitionService
{
    private readonly OfflineSpeechRecognitionService _offlineService;
    private readonly AzureSpeechRecognitionService _azureService;
    private readonly OpenAIWhisperSpeechRecognitionService _whisperService;
    private readonly HybridSpeechRecognitionService _hybridService;
    private readonly ISettingsService _settingsService;

    private ISpeechRecognitionService _activeService;
    private bool _isInitialized;
    private string _currentLanguage = "en-US";

    public event EventHandler<TranscriptionEventArgs>? RecognitionPartial;
    public event EventHandler<TranscriptionEventArgs>? RecognitionCompleted;
    public event EventHandler<RecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    public RecognitionState CurrentState => _activeService.CurrentState;
    public string ProviderName => _activeService.ProviderName;
    public bool IsAvailable => _activeService.IsAvailable;
    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Raised when the language changes.
    /// </summary>
    public event EventHandler<string>? LanguageChanged;

    public SpeechServiceManager(
        OfflineSpeechRecognitionService offlineService,
        AzureSpeechRecognitionService azureService,
        OpenAIWhisperSpeechRecognitionService whisperService,
        HybridSpeechRecognitionService hybridService,
        ISettingsService settingsService)
    {
        _offlineService = offlineService;
        _azureService = azureService;
        _whisperService = whisperService;
        _hybridService = hybridService;
        _settingsService = settingsService;

        // Start with the configured provider
        _activeService = GetServiceForProvider(settingsService.Current.SpeechProvider);

        // Wire up events from all services
        WireEvents(_offlineService);
        WireEvents(_azureService);
        WireEvents(_whisperService);
        WireEvents(_hybridService);

        // Listen for settings changes
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void WireEvents(ISpeechRecognitionService service)
    {
        service.RecognitionPartial += (s, e) => { if (s == _activeService) RecognitionPartial?.Invoke(this, e); };
        service.RecognitionCompleted += (s, e) => { if (s == _activeService) RecognitionCompleted?.Invoke(this, e); };
        service.RecognitionError += (s, e) => { if (s == _activeService) RecognitionError?.Invoke(this, e); };
        service.StateChanged += (s, e) => { if (s == _activeService) StateChanged?.Invoke(this, e); };
    }

    private ISpeechRecognitionService GetServiceForProvider(SpeechProvider provider)
    {
        return provider switch
        {
            SpeechProvider.Azure => _azureService,
            SpeechProvider.OpenAI => _whisperService,
            _ => _hybridService // Offline uses Hybrid for fallback support
        };
    }

    private async void OnSettingsChanged(object? sender, Models.AppSettings settings)
    {
        var newService = GetServiceForProvider(settings.SpeechProvider);
        var providerChanged = newService != _activeService;
        var languageChanged = settings.RecognitionLanguage != _currentLanguage;

        if (!providerChanged && !languageChanged)
            return;

        Log($"Settings changed - Provider: {providerChanged}, Language: {languageChanged} (new: {settings.RecognitionLanguage})");

        // Stop current recognition if active
        if (_activeService.CurrentState == RecognitionState.Listening)
        {
            await _activeService.StopRecognitionAsync();
        }

        // Configure the new service
        switch (settings.SpeechProvider)
        {
            case SpeechProvider.Azure:
                _azureService.Configure(settings.AzureSubscriptionKey, settings.AzureRegion);
                break;
            case SpeechProvider.OpenAI:
                _whisperService.Configure(settings.OpenAIApiKey ?? string.Empty);
                break;
        }

        // Update language
        var newLanguage = settings.RecognitionLanguage;

        // Re-initialize if needed (provider or language changed)
        if (_isInitialized && (providerChanged || languageChanged))
        {
            try
            {
                await newService.InitializeAsync(newLanguage);
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize provider with new settings: {ex.Message}");
                RecognitionError?.Invoke(this, new RecognitionErrorEventArgs($"Failed to apply settings: {ex.Message}", ex));
                return;
            }
        }

        _activeService = newService;

        // Update current language and notify
        if (languageChanged)
        {
            _currentLanguage = newLanguage;
            LanguageChanged?.Invoke(this, _currentLanguage);
            Log($"Language changed to {_currentLanguage}");
        }

        if (providerChanged)
        {
            Log($"Provider switched to {_activeService.ProviderName}");
        }

        // Notify UI of state change
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(RecognitionState.Idle, RecognitionState.Idle));
    }

    public async Task InitializeAsync(string language = "en-US")
    {
        _currentLanguage = language;
        var settings = _settingsService.Current;

        // Configure services
        _azureService.Configure(settings.AzureSubscriptionKey, settings.AzureRegion);
        _whisperService.Configure(settings.OpenAIApiKey ?? string.Empty);

        // Initialize the active service
        await _activeService.InitializeAsync(language);
        _isInitialized = true;

        Log($"SpeechServiceManager initialized with {_activeService.ProviderName}");
    }

    public Task StartRecognitionAsync(CancellationToken cancellationToken = default)
    {
        return _activeService.StartRecognitionAsync(cancellationToken);
    }

    public Task<string> StopRecognitionAsync()
    {
        return _activeService.StopRecognitionAsync();
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _offlineService.Dispose();
        _azureService.Dispose();
        _whisperService.Dispose();
        // Don't dispose hybrid separately as it wraps offline and azure
    }

    private static void Log(string message)
    {
        var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [ServiceManager] {message}";
        try { System.IO.File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }
}
