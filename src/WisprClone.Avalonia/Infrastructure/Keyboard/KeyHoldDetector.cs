using System;

namespace WisprClone.Infrastructure.Keyboard;

/// <summary>
/// Detector for key hold activation (e.g., hold Ctrl for 1 second).
/// Fires when a key is held down for a specified duration.
/// </summary>
public class KeyHoldDetector : IHotkeyDetector
{
    private readonly IGlobalKeyboardHook _hook;
    private readonly GlobalKeyCode _targetKey;
    private readonly int _holdDurationMs;
    private readonly Action<string>? _logAction;
    private readonly string _keyName;

    private System.Timers.Timer? _holdTimer;
    private bool _keyCurrentlyDown;
    private bool _disposed;
    private bool _started;
    private bool _fired; // Prevents multiple fires during a single hold

    /// <summary>
    /// Raised when the target key is held for the required duration.
    /// </summary>
    public event EventHandler? HotkeyActivated;

    /// <summary>
    /// Initializes a new instance of KeyHoldDetector.
    /// </summary>
    /// <param name="hook">The global keyboard hook to use.</param>
    /// <param name="targetKey">The key to detect hold for.</param>
    /// <param name="holdDurationMs">Duration in milliseconds to hold before activation.</param>
    /// <param name="logAction">Optional logging action for debugging.</param>
    public KeyHoldDetector(
        IGlobalKeyboardHook hook,
        GlobalKeyCode targetKey,
        int holdDurationMs = 1000,
        Action<string>? logAction = null)
    {
        _hook = hook;
        _targetKey = targetKey;
        _holdDurationMs = holdDurationMs;
        _logAction = logAction;
        _keyName = targetKey.ToString();

        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;

        Log($"Created detector for {_keyName}, holdDuration={holdDurationMs}ms");
    }

    /// <summary>
    /// Starts listening for key hold events.
    /// </summary>
    public void Start()
    {
        Log($"Start() called, _started={_started}");
        _started = true;
        _hook.Install();
        Log($"Hook installed for {_keyName}");
    }

    /// <summary>
    /// Stops listening for key hold events.
    /// </summary>
    public void Stop()
    {
        Log($"Stop() called");
        _started = false;
        StopHoldTimer();
        _hook.Uninstall();
    }

    /// <summary>
    /// Whether the detector is currently active.
    /// </summary>
    public bool IsActive => _started;

    private void Log(string message)
    {
        _logAction?.Invoke($"[{_keyName}Hold] {message}");
    }

    private bool IsTargetKey(GlobalKeyCode key)
    {
        // Handle both left and right variants of modifier keys
        return _targetKey switch
        {
            GlobalKeyCode.LeftCtrl or GlobalKeyCode.RightCtrl =>
                key == GlobalKeyCode.LeftCtrl ||
                key == GlobalKeyCode.RightCtrl ||
                key == GlobalKeyCode.LeftMeta ||
                key == GlobalKeyCode.RightMeta,

            GlobalKeyCode.LeftShift or GlobalKeyCode.RightShift =>
                key == GlobalKeyCode.LeftShift ||
                key == GlobalKeyCode.RightShift,

            GlobalKeyCode.LeftAlt or GlobalKeyCode.RightAlt =>
                key == GlobalKeyCode.LeftAlt ||
                key == GlobalKeyCode.RightAlt,

            _ => key == _targetKey
        };
    }

    private void OnKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        if (!_started || _disposed)
            return;

        if (!IsTargetKey(e.KeyCode) || _keyCurrentlyDown)
            return;

        _keyCurrentlyDown = true;
        _fired = false;
        Log($"KeyDown detected, starting hold timer ({_holdDurationMs}ms)");
        StartHoldTimer();
    }

    private void OnKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        if (!_started || _disposed)
            return;

        if (!IsTargetKey(e.KeyCode) || !_keyCurrentlyDown)
            return;

        _keyCurrentlyDown = false;
        Log($"KeyUp detected, cancelling hold timer (fired={_fired})");
        StopHoldTimer();
    }

    private void StartHoldTimer()
    {
        StopHoldTimer();

        _holdTimer = new System.Timers.Timer(_holdDurationMs);
        _holdTimer.AutoReset = false;
        _holdTimer.Elapsed += OnHoldTimerElapsed;
        _holdTimer.Start();
    }

    private void StopHoldTimer()
    {
        if (_holdTimer != null)
        {
            _holdTimer.Stop();
            _holdTimer.Elapsed -= OnHoldTimerElapsed;
            _holdTimer.Dispose();
            _holdTimer = null;
        }
    }

    private void OnHoldTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_started || _disposed || !_keyCurrentlyDown || _fired)
            return;

        _fired = true; // Prevent multiple fires
        Log($"HOLD DETECTED! Key held for {_holdDurationMs}ms, firing HotkeyActivated");
        HotkeyActivated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.KeyDown -= OnKeyDown;
        _hook.KeyUp -= OnKeyUp;
        StopHoldTimer();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~KeyHoldDetector()
    {
        Dispose();
    }
}
