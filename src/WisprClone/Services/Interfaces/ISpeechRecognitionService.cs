using WisprClone.Core;

namespace WisprClone.Services.Interfaces;

/// <summary>
/// Interface for speech recognition services.
/// </summary>
public interface ISpeechRecognitionService : IDisposable
{
    /// <summary>
    /// Raised when interim/partial recognition results are available.
    /// </summary>
    event EventHandler<TranscriptionEventArgs>? RecognitionPartial;

    /// <summary>
    /// Raised when final recognition result is available.
    /// </summary>
    event EventHandler<TranscriptionEventArgs>? RecognitionCompleted;

    /// <summary>
    /// Raised when recognition encounters an error.
    /// </summary>
    event EventHandler<RecognitionErrorEventArgs>? RecognitionError;

    /// <summary>
    /// Raised when the recognition state changes.
    /// </summary>
    event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Current state of the recognition service.
    /// </summary>
    RecognitionState CurrentState { get; }

    /// <summary>
    /// Name of the provider (Offline, Azure, Hybrid).
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Current recognition language code (e.g., "en-US", "ro-RO").
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Raised when the recognition language changes.
    /// </summary>
    event EventHandler<string>? LanguageChanged;

    /// <summary>
    /// Indicates if the service is available and properly configured.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Start continuous speech recognition.
    /// </summary>
    Task StartRecognitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop speech recognition and return final text.
    /// </summary>
    Task<string> StopRecognitionAsync();

    /// <summary>
    /// Initialize the recognition engine with specified language.
    /// </summary>
    Task InitializeAsync(string language = "en-US");
}
