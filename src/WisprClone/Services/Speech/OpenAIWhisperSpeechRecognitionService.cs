using NAudio.Wave;
using OpenAI;
using OpenAI.Audio;
using System.IO;
using System.Timers;
using WisprClone.Core;
using WisprClone.Services.Interfaces;
using Timer = System.Timers.Timer;

namespace WisprClone.Services.Speech;

/// <summary>
/// OpenAI Whisper API implementation with progressive transcription for real-time updates.
/// Sends the FULL accumulated audio every 2 seconds for context-aware transcription.
/// </summary>
public class OpenAIWhisperSpeechRecognitionService : ISpeechRecognitionService
{
    private string _apiKey = string.Empty;
    private string _language = "en";
    private WaveInEvent? _waveIn;
    private MemoryStream? _fullAudioStream;
    private WaveFileWriter? _fullAudioWriter;
    private bool _disposed;
    private bool _isRecording;

    private string _lastTranscription = string.Empty;
    private Timer? _transcriptionTimer;
    private readonly object _audioLock = new();
    private const int TranscriptionIntervalMs = 2000; // Transcribe every 2 seconds
    private const int MinAudioSize = 16000; // Minimum audio data (about 0.5s at 16kHz mono 16-bit)
    private const float SilenceThreshold = 500f; // RMS threshold for silence detection
    private int _lastProcessedLength = 0; // Track how much audio we've checked for silence

    public event EventHandler<TranscriptionEventArgs>? RecognitionPartial;
    public event EventHandler<TranscriptionEventArgs>? RecognitionCompleted;
    public event EventHandler<RecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;
    public event EventHandler<string>? LanguageChanged;

    public RecognitionState CurrentState { get; private set; } = RecognitionState.Idle;
    public string ProviderName => "OpenAI Whisper";
    public string CurrentLanguage { get; private set; } = "en-US";
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    /// <summary>
    /// Configures the OpenAI Whisper service with API key.
    /// </summary>
    public void Configure(string apiKey)
    {
        _apiKey = apiKey;
    }

    public Task InitializeAsync(string language = "en-US")
    {
        CurrentLanguage = language;
        // Convert language code to Whisper format (e.g., "en-US" -> "en")
        _language = language.Split('-')[0].ToLowerInvariant();
        return Task.CompletedTask;
    }

    public Task StartRecognitionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("OpenAI API key not configured");

        if (_isRecording)
            return Task.CompletedTask;

        try
        {
            UpdateState(RecognitionState.Initializing);

            // Clear state
            _lastTranscription = string.Empty;
            _lastProcessedLength = 0;

            // Initialize single continuous audio stream
            lock (_audioLock)
            {
                _fullAudioStream = new MemoryStream();
                _fullAudioWriter = new WaveFileWriter(
                    new IgnoreDisposeStream(_fullAudioStream),
                    new WaveFormat(16000, 16, 1));
            }

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, mono (optimal for Whisper)
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
            _isRecording = true;

            // Start timer for periodic full-audio transcription
            _transcriptionTimer = new Timer(TranscriptionIntervalMs);
            _transcriptionTimer.Elapsed += OnTranscriptionTimerElapsed;
            _transcriptionTimer.AutoReset = true;
            _transcriptionTimer.Start();

            UpdateState(RecognitionState.Listening);

            // Notify that we're listening
            RecognitionPartial?.Invoke(this, new TranscriptionEventArgs("Listening...", false));

            Log("Started recording with progressive full-audio transcription");
        }
        catch (Exception ex)
        {
            RecognitionError?.Invoke(this, new RecognitionErrorEventArgs(
                $"Failed to start audio recording: {ex.Message}", ex));
            UpdateState(RecognitionState.Error);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the new audio segment (since last check) contains actual speech, not just silence.
    /// </summary>
    private bool HasNewSpeechActivity(byte[] wavData)
    {
        // WAV header is 44 bytes, audio data starts after
        const int wavHeaderSize = 44;
        if (wavData.Length <= wavHeaderSize + _lastProcessedLength)
            return false;

        // Only check the NEW audio since last time
        int startOffset = wavHeaderSize + _lastProcessedLength;
        int newDataLength = wavData.Length - startOffset;

        if (newDataLength < 3200) // Less than 0.1s of new audio
            return false;

        // Calculate RMS of the new audio segment
        double sumSquares = 0;
        int sampleCount = 0;

        for (int i = startOffset; i < wavData.Length - 1; i += 2)
        {
            short sample = (short)(wavData[i] | (wavData[i + 1] << 8));
            sumSquares += sample * sample;
            sampleCount++;
        }

        if (sampleCount == 0)
            return false;

        double rms = Math.Sqrt(sumSquares / sampleCount);

        // Update the processed length for next check
        _lastProcessedLength = wavData.Length - wavHeaderSize;

        Log($"New audio segment RMS: {rms:F0} (threshold: {SilenceThreshold})");
        return rms > SilenceThreshold;
    }

    private async void OnTranscriptionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isRecording) return;

        try
        {
            byte[]? fullAudioData = null;

            lock (_audioLock)
            {
                if (_fullAudioWriter == null || _fullAudioStream == null)
                    return;

                _fullAudioWriter.Flush();
                fullAudioData = _fullAudioStream.ToArray();
            }

            // Only transcribe if we have enough audio and there's been speech activity
            if (fullAudioData == null || fullAudioData.Length < MinAudioSize)
                return;

            // Check if there's new speech activity (not just silence)
            if (!HasNewSpeechActivity(fullAudioData))
            {
                Log("Skipping transcription - no new speech detected");
                return;
            }

            Log($"Transcribing full audio: {fullAudioData.Length} bytes");
            var fullText = await TranscribeWithWhisperAsync(fullAudioData);

            // Only update if we got meaningful text that's different from before
            if (!string.IsNullOrWhiteSpace(fullText) && fullText != _lastTranscription)
            {
                _lastTranscription = fullText;
                Log($"Full transcription: '{fullText}'");
                RecognitionPartial?.Invoke(this, new TranscriptionEventArgs(fullText, false));
            }
        }
        catch (Exception ex)
        {
            Log($"Transcription error: {ex.Message}");
            // Don't fail the whole session for a single transcription error
        }
    }

    public async Task<string> StopRecognitionAsync()
    {
        if (!_isRecording || _waveIn == null)
            return string.Empty;

        try
        {
            UpdateState(RecognitionState.Processing);

            // Stop timer
            _transcriptionTimer?.Stop();
            _transcriptionTimer?.Dispose();
            _transcriptionTimer = null;

            // Stop recording
            _waveIn.StopRecording();
            _isRecording = false;

            // Do one final transcription of the complete audio
            byte[]? finalAudioData = null;
            lock (_audioLock)
            {
                if (_fullAudioWriter != null && _fullAudioStream != null)
                {
                    _fullAudioWriter.Flush();
                    finalAudioData = _fullAudioStream.ToArray();
                }
            }

            string finalText = _lastTranscription;

            if (finalAudioData != null && finalAudioData.Length > MinAudioSize)
            {
                Log($"Final transcription of full audio: {finalAudioData.Length} bytes");
                var transcribedText = await TranscribeWithWhisperAsync(finalAudioData);

                if (!string.IsNullOrWhiteSpace(transcribedText))
                {
                    finalText = transcribedText;
                }
            }

            Log($"Final result: '{finalText}'");

            RecognitionCompleted?.Invoke(this, new TranscriptionEventArgs(finalText, true));
            UpdateState(RecognitionState.Idle);

            return finalText;
        }
        catch (Exception ex)
        {
            RecognitionError?.Invoke(this, new RecognitionErrorEventArgs(
                $"Failed to transcribe audio: {ex.Message}", ex));
            UpdateState(RecognitionState.Error);
            return _lastTranscription; // Return what we have so far
        }
        finally
        {
            CleanupRecording();
        }
    }

    private async Task<string> TranscribeWithWhisperAsync(byte[] audioData)
    {
        try
        {
            var client = new OpenAIClient(_apiKey);
            var audioClient = client.GetAudioClient("whisper-1");

            using var audioStream = new MemoryStream(audioData);

            var options = new AudioTranscriptionOptions
            {
                Language = _language,
                ResponseFormat = AudioTranscriptionFormat.Text
            };

            var result = await audioClient.TranscribeAudioAsync(
                audioStream,
                "audio.wav",
                options);

            return result.Value.Text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log($"Whisper API error: {ex.Message}");
            throw;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_audioLock)
        {
            if (_fullAudioWriter != null && _isRecording)
            {
                _fullAudioWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            RecognitionError?.Invoke(this, new RecognitionErrorEventArgs(
                $"Recording error: {e.Exception.Message}", e.Exception));
        }
    }

    private void CleanupRecording()
    {
        _transcriptionTimer?.Stop();
        _transcriptionTimer?.Dispose();
        _transcriptionTimer = null;

        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        lock (_audioLock)
        {
            _fullAudioWriter?.Dispose();
            _fullAudioWriter = null;

            _fullAudioStream?.Dispose();
            _fullAudioStream = null;
        }
    }

    private void UpdateState(RecognitionState newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(oldState, newState));
    }

    private static void Log(string message)
    {
        var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [Whisper] {message}";
        try { System.IO.File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isRecording)
        {
            _waveIn?.StopRecording();
            _isRecording = false;
        }

        CleanupRecording();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~OpenAIWhisperSpeechRecognitionService()
    {
        Dispose();
    }

    /// <summary>
    /// Helper stream that ignores Dispose calls (needed for WaveFileWriter with MemoryStream).
    /// </summary>
    private class IgnoreDisposeStream : Stream
    {
        private readonly Stream _inner;

        public IgnoreDisposeStream(Stream inner) => _inner = inner;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            // Don't dispose the inner stream
        }
    }
}
