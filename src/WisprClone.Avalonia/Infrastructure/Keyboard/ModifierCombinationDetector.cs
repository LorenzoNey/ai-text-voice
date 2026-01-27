using System;

namespace WisprClone.Infrastructure.Keyboard;

/// <summary>
/// Detector for modifier key combinations (e.g., Ctrl+Space, Ctrl+Shift+F9).
/// Fires when the primary key is pressed while required modifiers are held.
/// </summary>
public class ModifierCombinationDetector : IHotkeyDetector
{
    private readonly IGlobalKeyboardHook _hook;
    private readonly GlobalKeyCode _primaryKey;
    private readonly bool _requireCtrl;
    private readonly bool _requireShift;
    private readonly bool _requireAlt;
    private readonly Action<string>? _logAction;
    private readonly string _displayName;

    // Track modifier state
    private bool _ctrlDown;
    private bool _shiftDown;
    private bool _altDown;

    private bool _disposed;
    private bool _started;

    /// <summary>
    /// Raised when the key combination is detected.
    /// </summary>
    public event EventHandler? HotkeyActivated;

    /// <summary>
    /// Initializes a new instance of ModifierCombinationDetector.
    /// </summary>
    /// <param name="hook">The global keyboard hook to use.</param>
    /// <param name="primaryKey">The primary key (e.g., Space, F9).</param>
    /// <param name="modifiers">Required modifiers as a string (e.g., "Ctrl", "Ctrl+Shift").</param>
    /// <param name="logAction">Optional logging action for debugging.</param>
    public ModifierCombinationDetector(
        IGlobalKeyboardHook hook,
        GlobalKeyCode primaryKey,
        string modifiers,
        Action<string>? logAction = null)
    {
        _hook = hook;
        _primaryKey = primaryKey;
        _logAction = logAction;

        // Parse modifiers
        var modifierUpper = modifiers.ToUpperInvariant();
        _requireCtrl = modifierUpper.Contains("CTRL");
        _requireShift = modifierUpper.Contains("SHIFT");
        _requireAlt = modifierUpper.Contains("ALT");

        _displayName = $"{modifiers}+{primaryKey}";

        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;

        Log($"Created detector for {_displayName}");
    }

    /// <summary>
    /// Starts listening for combination events.
    /// </summary>
    public void Start()
    {
        Log($"Start() called, _started={_started}");
        _started = true;
        _hook.Install();
        Log($"Hook installed for {_displayName}");
    }

    /// <summary>
    /// Stops listening for combination events.
    /// </summary>
    public void Stop()
    {
        Log($"Stop() called");
        _started = false;
        _ctrlDown = false;
        _shiftDown = false;
        _altDown = false;
        _hook.Uninstall();
    }

    /// <summary>
    /// Whether the detector is currently active.
    /// </summary>
    public bool IsActive => _started;

    private void Log(string message)
    {
        _logAction?.Invoke($"[{_displayName}Combo] {message}");
    }

    private void OnKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        if (!_started || _disposed)
            return;

        // Track modifier state
        UpdateModifierState(e.KeyCode, isDown: true);

        // Check if primary key is pressed
        if (e.KeyCode != _primaryKey)
            return;

        // Check if all required modifiers are held
        var modifiersMatch =
            (!_requireCtrl || _ctrlDown) &&
            (!_requireShift || _shiftDown) &&
            (!_requireAlt || _altDown);

        // Also ensure no extra modifiers are pressed (only the ones we need)
        var noExtraModifiers =
            (_requireCtrl || !_ctrlDown) &&
            (_requireShift || !_shiftDown) &&
            (_requireAlt || !_altDown);

        if (modifiersMatch && noExtraModifiers)
        {
            Log($"COMBINATION DETECTED! {_displayName}, firing HotkeyActivated");
            HotkeyActivated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Log($"Primary key pressed but modifiers don't match: Ctrl={_ctrlDown}, Shift={_shiftDown}, Alt={_altDown}");
        }
    }

    private void OnKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        if (!_started || _disposed)
            return;

        // Track modifier state
        UpdateModifierState(e.KeyCode, isDown: false);
    }

    private void UpdateModifierState(GlobalKeyCode key, bool isDown)
    {
        switch (key)
        {
            case GlobalKeyCode.LeftCtrl:
            case GlobalKeyCode.RightCtrl:
            case GlobalKeyCode.LeftMeta:  // Treat Command as Ctrl on macOS
            case GlobalKeyCode.RightMeta:
                _ctrlDown = isDown;
                break;

            case GlobalKeyCode.LeftShift:
            case GlobalKeyCode.RightShift:
                _shiftDown = isDown;
                break;

            case GlobalKeyCode.LeftAlt:
            case GlobalKeyCode.RightAlt:
                _altDown = isDown;
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.KeyDown -= OnKeyDown;
        _hook.KeyUp -= OnKeyUp;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ModifierCombinationDetector()
    {
        Dispose();
    }
}
