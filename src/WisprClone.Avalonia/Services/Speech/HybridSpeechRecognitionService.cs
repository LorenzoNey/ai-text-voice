#if WINDOWS
using WisprClone.Core;
using WisprClone.Services.Interfaces;

namespace WisprClone.Services.Speech;

/// <summary>
/// Hybrid speech recognition service that uses offline recognition as primary
/// and falls back to Azure on consecutive errors (Windows only).
/// </summary>
public class HybridSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly OfflineSpeechRecognitionService _offlineService;
    private readonly AzureSpeechRecognitionService _azureService;
    private readonly ISettingsService _settingsService;

    private ISpeechRecognitionService _activeService;
    private int _consecutiveErrors;
    private bool _disposed;

    public event EventHandler<TranscriptionEventArgs>? RecognitionPartial;
    public event EventHandler<TranscriptionEventArgs>? RecognitionCompleted;
    public event EventHandler<RecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;
    public event EventHandler<string>? LanguageChanged;

    public RecognitionState CurrentState => _activeService.CurrentState;
    public string ProviderName => $"Hybrid ({_activeService.ProviderName})";
    public string CurrentLanguage => _activeService.CurrentLanguage;
    public bool IsAvailable => _offlineService.IsAvailable || _azureService.IsAvailable;

    public HybridSpeechRecognitionService(
        OfflineSpeechRecognitionService offlineService,
        AzureSpeechRecognitionService azureService,
        ISettingsService settingsService)
    {
        _offlineService = offlineService;
        _azureService = azureService;
        _settingsService = settingsService;
        _activeService = offlineService;

        WireEvents(_offlineService);
        WireEvents(_azureService);
    }

    private void WireEvents(ISpeechRecognitionService service)
    {
        service.RecognitionPartial += OnServiceRecognitionPartial;
        service.RecognitionCompleted += OnServiceRecognitionCompleted;
        service.StateChanged += OnServiceStateChanged;
        service.RecognitionError += OnServiceError;
    }

    private void UnwireEvents(ISpeechRecognitionService service)
    {
        service.RecognitionPartial -= OnServiceRecognitionPartial;
        service.RecognitionCompleted -= OnServiceRecognitionCompleted;
        service.StateChanged -= OnServiceStateChanged;
        service.RecognitionError -= OnServiceError;
    }

    private void OnServiceRecognitionPartial(object? sender, TranscriptionEventArgs e)
    {
        if (sender == _activeService)
            RecognitionPartial?.Invoke(this, e);
    }

    private void OnServiceRecognitionCompleted(object? sender, TranscriptionEventArgs e)
    {
        if (sender == _activeService)
        {
            _consecutiveErrors = 0;
            RecognitionCompleted?.Invoke(this, e);
        }
    }

    private void OnServiceStateChanged(object? sender, RecognitionStateChangedEventArgs e)
    {
        if (sender == _activeService)
            StateChanged?.Invoke(this, e);
    }

    private void OnServiceError(object? sender, RecognitionErrorEventArgs e)
    {
        if (sender != _activeService) return;

        _consecutiveErrors++;

        if (_consecutiveErrors >= Constants.MaxErrorsBeforeFallback &&
            _activeService == _offlineService &&
            _azureService.IsAvailable &&
            _settingsService.Current.UseAzureFallback)
        {
            SwitchToAzure();
            RecognitionError?.Invoke(this, new RecognitionErrorEventArgs(
                "Switched to Azure Speech Service due to repeated errors", null));
        }
        else
        {
            RecognitionError?.Invoke(this, e);
        }
    }

    public async Task InitializeAsync(string language = "en-US")
    {
        await _offlineService.InitializeAsync(language);

        var settings = _settingsService.Current;
        if (!string.IsNullOrEmpty(settings.AzureSubscriptionKey) &&
            !string.IsNullOrEmpty(settings.AzureRegion))
        {
            _azureService.Configure(settings.AzureSubscriptionKey, settings.AzureRegion);

            if (settings.UseAzureFallback)
            {
                try
                {
                    await _azureService.InitializeAsync(language);
                }
                catch { /* Azure init failure is non-fatal */ }
            }
        }
    }

    public Task StartRecognitionAsync(CancellationToken cancellationToken = default)
        => _activeService.StartRecognitionAsync(cancellationToken);

    public async Task<string> StopRecognitionAsync()
    {
        var result = await _activeService.StopRecognitionAsync();
        _consecutiveErrors = 0;
        return result;
    }

    public void SwitchToAzure()
    {
        if (_azureService.IsAvailable)
        {
            _activeService = _azureService;
            _consecutiveErrors = 0;
        }
    }

    public void SwitchToOffline()
    {
        _activeService = _offlineService;
        _consecutiveErrors = 0;
    }

    public ISpeechRecognitionService ActiveService => _activeService;

    public void Dispose()
    {
        if (_disposed) return;

        UnwireEvents(_offlineService);
        UnwireEvents(_azureService);

        _offlineService.Dispose();
        _azureService.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~HybridSpeechRecognitionService() => Dispose();
}
#endif
