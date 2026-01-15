using System.Globalization;
using System.Speech.Recognition;
using System.Text;
using WisprClone.Core;
using WisprClone.Services.Interfaces;

namespace WisprClone.Services.Speech;

/// <summary>
/// Offline speech recognition service using System.Speech.
/// </summary>
public class OfflineSpeechRecognitionService : ISpeechRecognitionService
{
    private SpeechRecognitionEngine? _recognizer;
    private readonly StringBuilder _transcriptionBuffer = new();
    private string _lastDisplayedText = string.Empty;
    private string _currentHypothesis = string.Empty;
    private bool _isRecognizing;
    private bool _disposed;
    private Task? _resetTask;

    public event EventHandler<TranscriptionEventArgs>? RecognitionPartial;
    public event EventHandler<TranscriptionEventArgs>? RecognitionCompleted;
    public event EventHandler<RecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;
    public event EventHandler<string>? LanguageChanged;

    public RecognitionState CurrentState { get; private set; } = RecognitionState.Idle;
    public string ProviderName => "Offline (System.Speech)";
    public string CurrentLanguage { get; private set; } = "en-US";
    public bool IsAvailable => true; // Always available on Windows

    public Task InitializeAsync(string language = "en-US")
    {
        try
        {
            CurrentLanguage = language;
            var culture = new CultureInfo(language);
            _recognizer = new SpeechRecognitionEngine(culture);

            // Use dictation grammar for free-form speech
            var dictationGrammar = new DictationGrammar
            {
                Name = "Dictation Grammar"
            };
            _recognizer.LoadGrammar(dictationGrammar);

            // Configure for microphone input
            _recognizer.SetInputToDefaultAudioDevice();

            // Configure longer timeouts to prevent premature speech cutoffs
            _recognizer.EndSilenceTimeout = TimeSpan.FromSeconds(1.5);
            _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(2.0);

            // Wire up events
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechHypothesized += OnSpeechHypothesized;
            _recognizer.RecognizeCompleted += OnRecognizeCompleted;
            _recognizer.SpeechRecognitionRejected += OnSpeechRejected;

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            RecognitionError?.Invoke(this, new RecognitionErrorEventArgs(
                $"Failed to initialize offline recognition: {ex.Message}", ex));
            throw;
        }
    }

    public async Task StartRecognitionAsync(CancellationToken cancellationToken = default)
    {
        Log("StartRecognitionAsync called");

        if (_recognizer == null)
            throw new InvalidOperationException("Recognizer not initialized");

        // Wait for any pending reset to complete
        if (_resetTask != null)
        {
            Log("Waiting for reset task...");
            await _resetTask;
            _resetTask = null;
            Log("Reset task completed");
        }

        _transcriptionBuffer.Clear();
        _lastDisplayedText = string.Empty;
        _currentHypothesis = string.Empty;
        _isRecognizing = true;

        Log("Calling RecognizeAsync...");
        UpdateState(RecognitionState.Listening);

        // Use RecognizeAsync with multiple recognition mode for continuous recognition
        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        Log("RecognizeAsync called");
    }

    public Task<string> StopRecognitionAsync()
    {
        Log("StopRecognitionAsync called");

        if (_recognizer == null || !_isRecognizing)
            return Task.FromResult(string.Empty);

        // Use Cancel for immediate termination
        Log("Calling RecognizeAsyncCancel...");
        _recognizer.RecognizeAsyncCancel();
        Log("RecognizeAsyncCancel completed");
        _isRecognizing = false;

        // Use the last displayed text which includes any pending hypothesis
        var finalText = _lastDisplayedText.Trim();
        RecognitionCompleted?.Invoke(this, new TranscriptionEventArgs(finalText, true));

        // Go to Idle immediately so UI updates right away
        UpdateState(RecognitionState.Idle);
        Log("State set to Idle, starting reset task...");

        // Reset audio input on background thread to make engine ready for next session
        // Store the task so StartRecognitionAsync can wait for it if needed
        _resetTask = Task.Run(() =>
        {
            Log("Reset task started");
            try
            {
                _recognizer?.SetInputToNull();
                Log("SetInputToNull done");
                _recognizer?.SetInputToDefaultAudioDevice();
                Log("SetInputToDefaultAudioDevice done");
            }
            catch (Exception ex)
            {
                Log($"Reset error: {ex.Message}");
            }
            Log("Reset task finished");
        });

        Log("StopRecognitionAsync returning");
        return Task.FromResult(finalText);
    }

    private static void Log(string message)
    {
        var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [Speech] {message}";
        try { System.IO.File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }

    private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        // Store current hypothesis
        _currentHypothesis = e.Result.Text;

        // Show buffer + hypothesis
        var currentText = _transcriptionBuffer.ToString() + _currentHypothesis;

        // Never show less text than before (prevents flickering/resetting)
        if (currentText.Length >= _lastDisplayedText.Length)
        {
            _lastDisplayedText = currentText;
        }

        RecognitionPartial?.Invoke(this, new TranscriptionEventArgs(_lastDisplayedText, false));
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence > Constants.MinConfidenceThreshold)
        {
            // Append finalized text to buffer
            _transcriptionBuffer.Append(e.Result.Text + " ");
            _currentHypothesis = string.Empty;

            // Update displayed text
            var currentText = _transcriptionBuffer.ToString();
            _lastDisplayedText = currentText;

            RecognitionPartial?.Invoke(this, new TranscriptionEventArgs(currentText, false));
        }
    }

    private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
    {
        // Low confidence recognition - could log or notify
        // We don't raise an error for rejected speech, just ignore it
    }

    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            RecognitionError?.Invoke(this,
                new RecognitionErrorEventArgs(e.Error.Message, e.Error));
            UpdateState(RecognitionState.Error);
        }
    }

    private void UpdateState(RecognitionState newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(oldState, newState));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_recognizer != null)
        {
            if (_isRecognizing)
            {
                _recognizer.RecognizeAsyncStop();
            }

            _recognizer.SpeechRecognized -= OnSpeechRecognized;
            _recognizer.SpeechHypothesized -= OnSpeechHypothesized;
            _recognizer.RecognizeCompleted -= OnRecognizeCompleted;
            _recognizer.SpeechRecognitionRejected -= OnSpeechRejected;
            _recognizer.Dispose();
            _recognizer = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~OfflineSpeechRecognitionService()
    {
        Dispose();
    }
}
