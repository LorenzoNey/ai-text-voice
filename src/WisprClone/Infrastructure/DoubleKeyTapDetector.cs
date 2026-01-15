using System.Windows.Input;
using WisprClone.Core;

namespace WisprClone.Infrastructure;

/// <summary>
/// Detects double-tap of a specific key (e.g., Ctrl key).
/// </summary>
public class DoubleKeyTapDetector : IDisposable
{
    private readonly LowLevelKeyboardHook _hook;
    private readonly Key _targetKey;
    private readonly int _maxIntervalMs;
    private readonly int _maxHoldDurationMs;

    private DateTime _lastKeyDownTime = DateTime.MinValue;
    private DateTime _lastKeyUpTime = DateTime.MinValue;
    private int _tapCount;
    private bool _keyCurrentlyDown;
    private bool _disposed;

    /// <summary>
    /// Raised when a double-tap is detected.
    /// </summary>
    public event EventHandler? DoubleTapDetected;

    /// <summary>
    /// Initializes a new instance of the DoubleKeyTapDetector.
    /// </summary>
    /// <param name="targetKey">The key to detect double-tap for (default: LeftCtrl).</param>
    /// <param name="maxIntervalMs">Maximum interval between taps in milliseconds.</param>
    /// <param name="maxHoldDurationMs">Maximum hold duration to count as a tap.</param>
    public DoubleKeyTapDetector(
        Key targetKey = Key.LeftCtrl,
        int maxIntervalMs = Constants.DefaultDoubleTapIntervalMs,
        int maxHoldDurationMs = Constants.DefaultMaxKeyHoldDurationMs)
    {
        _targetKey = targetKey;
        _maxIntervalMs = maxIntervalMs;
        _maxHoldDurationMs = maxHoldDurationMs;

        _hook = new LowLevelKeyboardHook();
        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;
    }

    /// <summary>
    /// Starts listening for double-tap events.
    /// </summary>
    public void Start()
    {
        _hook.Install();
    }

    /// <summary>
    /// Stops listening for double-tap events.
    /// </summary>
    public void Stop()
    {
        _hook.Uninstall();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Check for Ctrl key (both left and right)
        if (!IsTargetKey(e.Key) || _keyCurrentlyDown)
            return;

        _keyCurrentlyDown = true;
        var now = DateTime.UtcNow;

        // Check if this is within the double-tap window
        var timeSinceLastKeyUp = (now - _lastKeyUpTime).TotalMilliseconds;

        if (timeSinceLastKeyUp > _maxIntervalMs)
        {
            // Too much time passed, reset count
            _tapCount = 0;
        }

        _lastKeyDownTime = now;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!IsTargetKey(e.Key) || !_keyCurrentlyDown)
            return;

        _keyCurrentlyDown = false;
        var now = DateTime.UtcNow;

        // Check if the key was held too long (not a tap)
        var holdDuration = (now - _lastKeyDownTime).TotalMilliseconds;
        if (holdDuration > _maxHoldDurationMs)
        {
            _tapCount = 0;
            return;
        }

        _tapCount++;
        _lastKeyUpTime = now;

        if (_tapCount >= 2)
        {
            _tapCount = 0;
            DoubleTapDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool IsTargetKey(Key key)
    {
        // Handle both left and right Ctrl
        return key == _targetKey ||
               key == Key.LeftCtrl ||
               key == Key.RightCtrl;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.KeyDown -= OnKeyDown;
        _hook.KeyUp -= OnKeyUp;
        _hook.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~DoubleKeyTapDetector()
    {
        Dispose();
    }
}
