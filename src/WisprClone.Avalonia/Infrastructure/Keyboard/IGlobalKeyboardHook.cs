using System;

namespace WisprClone.Infrastructure.Keyboard;

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
    LeftCtrl,
    RightCtrl,
    LeftAlt,
    RightAlt,
    LeftShift,
    RightShift,
    LeftMeta,  // Command key on macOS, Windows key on Windows
    RightMeta, // Command key on macOS, Windows key on Windows
    Space,
    Enter,
    Escape,
    Tab
}
