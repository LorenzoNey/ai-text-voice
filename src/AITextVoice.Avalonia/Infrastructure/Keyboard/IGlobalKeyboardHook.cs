using System;

namespace AITextVoice.Infrastructure.Keyboard;

/// <summary>
/// Platform-agnostic interface for global keyboard hooks.
/// Allows intercepting keyboard events system-wide.
/// </summary>
public interface IGlobalKeyboardHook : IDisposable
{
    /// <summary>
    /// Fired when a key is pressed down.
    /// </summary>
    event EventHandler<GlobalKeyEventArgs>? KeyDown;

    /// <summary>
    /// Fired when a key is released.
    /// </summary>
    event EventHandler<GlobalKeyEventArgs>? KeyUp;

    /// <summary>
    /// Install the keyboard hook to start receiving events.
    /// </summary>
    void Install();

    /// <summary>
    /// Uninstall the keyboard hook to stop receiving events.
    /// </summary>
    void Uninstall();

    /// <summary>
    /// Whether the hook is currently installed.
    /// </summary>
    bool IsInstalled { get; }
}

/// <summary>
/// Event args for keyboard events.
/// </summary>
public class GlobalKeyEventArgs : EventArgs
{
    public GlobalKeyCode KeyCode { get; }
    public DateTime Timestamp { get; }

    public GlobalKeyEventArgs(GlobalKeyCode keyCode)
    {
        KeyCode = keyCode;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Platform-agnostic key codes for common keys.
/// </summary>
public enum GlobalKeyCode
{
    Unknown = 0,

    // Modifier keys
    LeftCtrl,
    RightCtrl,
    LeftAlt,
    RightAlt,
    LeftShift,
    RightShift,
    LeftMeta,  // Command key on macOS, Windows key on Windows
    RightMeta, // Command key on macOS, Windows key on Windows

    // Common keys
    Space,
    Enter,
    Escape,
    Tab,

    // Function keys (F1-F12)
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,

    // Extended function keys (F13-F24) - rarely used by OS, good for hotkeys
    F13,
    F14,
    F15,
    F16,
    F17,
    F18,
    F19,
    F20,
    F21,
    F22,
    F23,
    F24,

    // Lock keys
    CapsLock,
    ScrollLock,
    NumLock,

    // Other keys
    Pause,
    PrintScreen,
    Insert,
    Delete,
    Home,
    End,
    PageUp,
    PageDown
}
