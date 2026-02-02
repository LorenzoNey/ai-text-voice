#if WINDOWS
using NAudio.Wave;
#endif

namespace AITextVoice.Services;

/// <summary>
/// Monitors microphone audio levels in real-time.
/// Currently requires Windows for NAudio support.
/// TODO: Add cross-platform audio monitoring.
/// </summary>
public class AudioLevelMonitor : IDisposable
{
#if WINDOWS
    private WaveInEvent? _waveIn;
#endif
    private bool _isMonitoring;
    private bool _disposed;
    private readonly object _lock = new();

    // Store recent samples for multiple bars (creates waveform effect)
    private readonly float[] _levelHistory = new float[5];
    private int _historyIndex;

    /// <summary>
    /// Fired when audio level changes. Value is 0.0 to 1.0.
    /// </summary>
    public event EventHandler<AudioLevelEventArgs>? LevelChanged;

    /// <summary>
    /// Starts monitoring audio levels from the default microphone.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isMonitoring) return;

#if WINDOWS
            try
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 50 // Update every 50ms for smooth visualization
                };

                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.StartRecording();
                _isMonitoring = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioLevelMonitor start error: {ex.Message}");
            }
#else
            // On non-Windows platforms, just mark as monitoring
            // Audio level visualization won't be available
            _isMonitoring = true;
            // Send initial level event with zeroes
            LevelChanged?.Invoke(this, new AudioLevelEventArgs(0.2f, 0.3f, 0.4f, 0.3f, 0.2f));
#endif
        }
    }

    /// <summary>
    /// Stops monitoring audio levels.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isMonitoring) return;

#if WINDOWS
            try
            {
                _waveIn?.StopRecording();
                _waveIn?.Dispose();
                _waveIn = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioLevelMonitor stop error: {ex.Message}");
            }
#endif

            _isMonitoring = false;

            // Reset levels
            Array.Clear(_levelHistory);
            LevelChanged?.Invoke(this, new AudioLevelEventArgs(0, 0, 0, 0, 0));
        }
    }

#if WINDOWS
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // Calculate RMS level from the audio buffer
        float sum = 0;
        int sampleCount = e.BytesRecorded / 2; // 16-bit samples

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            float normalized = sample / 32768f;
            sum += normalized * normalized;
        }

        float rms = sampleCount > 0 ? (float)Math.Sqrt(sum / sampleCount) : 0;

        // Apply some amplification and clamp to 0-1 range
        float level = Math.Min(1.0f, rms * 4f);

        // Store in history for waveform effect
        _levelHistory[_historyIndex] = level;
        _historyIndex = (_historyIndex + 1) % _levelHistory.Length;

        // Get levels for each bar (offset in history creates wave motion)
        var levels = new float[5];
        for (int i = 0; i < 5; i++)
        {
            int idx = (_historyIndex + i) % _levelHistory.Length;
            levels[i] = _levelHistory[idx];
        }

        LevelChanged?.Invoke(this, new AudioLevelEventArgs(
            levels[0], levels[1], levels[2], levels[3], levels[4]));
    }
#endif

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~AudioLevelMonitor()
    {
        Dispose();
    }
}

/// <summary>
/// Event args for audio level changes.
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    public float Level1 { get; }
    public float Level2 { get; }
    public float Level3 { get; }
    public float Level4 { get; }
    public float Level5 { get; }

    public AudioLevelEventArgs(float l1, float l2, float l3, float l4, float l5)
    {
        Level1 = l1;
        Level2 = l2;
        Level3 = l3;
        Level4 = l4;
        Level5 = l5;
    }
}
