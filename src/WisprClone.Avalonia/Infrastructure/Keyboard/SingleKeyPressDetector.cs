using System;

namespace WisprClone.Infrastructure.Keyboard;

/// <summary>
/// Detector for single key press activation (e.g., F9).
/// Fires on KeyDown for the target key.
/// </summary>
public class SingleKeyPressDetector : IHotkeyDetector
{
    private readonly IGlobalKeyboardHook _hook;
    private readonly GlobalKeyCode _targetKey;
    private readonly Action<string>? _logAction;
    private readonly string _keyName;

    private bool _disposed;
    private bool _started;

    /// <summary>
    /// Raised when the target key is pressed.
    /// </summary>
    public event EventHandler? HotkeyActivated;

    /// <summary>
    /// Initializes a new instance of SingleKeyPressDetector.
    /// </summary>
    /// <param name="hook">The global keyboard hook to use.</param>
    /// <param name="targetKey">The key to detect press for.</param>
    /// <param name="logAction">Optional logging action for debugging.</param>
    public SingleKeyPressDetector(
        IGlobalKeyboardHook hook,
        GlobalKeyCode targetKey,
        Action<string>? logAction = null)
    {
        _hook = hook;
        _targetKey = targetKey;
        _logAction = logAction;
        _keyName = targetKey.ToString();

        _hook.KeyDown += OnKeyDown;

        Log($"Created detector for {_keyName}");
    }

    /// <summary>
    /// Starts listening for key press events.
    /// </summary>
    public void Start()
    {
        Log($"Start() called, _started={_started}");
        _started = true;
        _hook.Install();
        Log($"Hook installed for {_keyName}");
    }

    /// <summary>
    /// Stops listening for key press events.
    /// </summary>
    public void Stop()
    {
        Log($"Stop() called");
        _started = false;
        _hook.Uninstall();
    }

    /// <summary>
    /// Whether the detector is currently active.
    /// </summary>
    public bool IsActive => _started;

    private void Log(string message)
    {
        _logAction?.Invoke($"[{_keyName}SinglePress] {message}");
    }

    private void OnKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        if (!_started || _disposed)
            return;

        if (e.KeyCode != _targetKey)
            return;

        Log($"KeyDown detected for {_keyName}, firing HotkeyActivated");
        HotkeyActivated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.KeyDown -= OnKeyDown;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SingleKeyPressDetector()
    {
        Dispose();
    }
}
