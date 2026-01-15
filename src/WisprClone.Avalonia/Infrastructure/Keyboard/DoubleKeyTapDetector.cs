using System;
using WisprClone.Core;

namespace WisprClone.Infrastructure.Keyboard;

/// <summary>
/// Cross-platform detector for double-tap of a specific key (e.g., Ctrl key).
/// </summary>
public class DoubleKeyTapDetector : IDisposable
{
    private readonly IGlobalKeyboardHook _hook;
    private readonly GlobalKeyCode _targetKey;
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
    /// <param name="hook">The global keyboard hook to use.</param>
    /// <param name="targetKey">The key to detect double-tap for (default: LeftCtrl).</param>
    /// <param name="maxIntervalMs">Maximum interval between taps in milliseconds.</param>
    /// <param name="maxHoldDurationMs">Maximum hold duration to count as a tap.</param>
    public DoubleKeyTapDetector(
        IGlobalKeyboardHook hook,
        GlobalKeyCode targetKey = GlobalKeyCode.LeftCtrl,
        int maxIntervalMs = Constants.DefaultDoubleTapIntervalMs,
        int maxHoldDurationMs = Constants.DefaultMaxKeyHoldDurationMs)
    {
        _hook = hook;
        _targetKey = targetKey;
        _maxIntervalMs = maxIntervalMs;
        _maxHoldDurationMs = maxHoldDurationMs;

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

    private void OnKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        // Check for Ctrl key (both left and right)
        if (!IsTargetKey(e.KeyCode) || _keyCurrentlyDown)
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

    private void OnKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        if (!IsTargetKey(e.KeyCode) || !_keyCurrentlyDown)
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

    private bool IsTargetKey(GlobalKeyCode key)
    {
        // Handle both left and right Ctrl
        return key == _targetKey ||
               key == GlobalKeyCode.LeftCtrl ||
               key == GlobalKeyCode.RightCtrl;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.KeyDown -= OnKeyDown;
        _hook.KeyUp -= OnKeyUp;
        // Note: Don't dispose the hook here as it's injected and may be shared
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~DoubleKeyTapDetector()
    {
        Dispose();
    }
}
