using System;
using SharpHook;
using SharpHook.Native;

namespace AITextVoice.Infrastructure.Keyboard;

/// <summary>
/// Cross-platform global keyboard hook implementation using SharpHook.
/// Works on Windows, macOS, and Linux.
/// </summary>
public class SharpHookKeyboardHook : IGlobalKeyboardHook
{
    private TaskPoolGlobalHook? _hook;
    private bool _disposed;

    public event EventHandler<GlobalKeyEventArgs>? KeyDown;
    public event EventHandler<GlobalKeyEventArgs>? KeyUp;

    public bool IsInstalled => _hook != null;

    public void Install()
    {
        if (_hook != null)
            return;

        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.RunAsync();
    }

    public void Uninstall()
    {
        if (_hook == null)
            return;

        _hook.KeyPressed -= OnKeyPressed;
        _hook.KeyReleased -= OnKeyReleased;
        _hook.Dispose();
        _hook = null;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = MapKeyCode(e.Data.KeyCode);
        if (keyCode != GlobalKeyCode.Unknown)
        {
            KeyDown?.Invoke(this, new GlobalKeyEventArgs(keyCode));
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = MapKeyCode(e.Data.KeyCode);
        if (keyCode != GlobalKeyCode.Unknown)
        {
            KeyUp?.Invoke(this, new GlobalKeyEventArgs(keyCode));
        }
    }

    private static GlobalKeyCode MapKeyCode(KeyCode nativeCode)
    {
        return nativeCode switch
        {
            // Modifier keys
            KeyCode.VcLeftControl => GlobalKeyCode.LeftCtrl,
            KeyCode.VcRightControl => GlobalKeyCode.RightCtrl,
            KeyCode.VcLeftAlt => GlobalKeyCode.LeftAlt,
            KeyCode.VcRightAlt => GlobalKeyCode.RightAlt,
            KeyCode.VcLeftShift => GlobalKeyCode.LeftShift,
            KeyCode.VcRightShift => GlobalKeyCode.RightShift,
            KeyCode.VcLeftMeta => GlobalKeyCode.LeftMeta,   // Command on macOS
            KeyCode.VcRightMeta => GlobalKeyCode.RightMeta, // Command on macOS

            // Common keys
            KeyCode.VcSpace => GlobalKeyCode.Space,
            KeyCode.VcEnter => GlobalKeyCode.Enter,
            KeyCode.VcEscape => GlobalKeyCode.Escape,
            KeyCode.VcTab => GlobalKeyCode.Tab,

            // Function keys F1-F12
            KeyCode.VcF1 => GlobalKeyCode.F1,
            KeyCode.VcF2 => GlobalKeyCode.F2,
            KeyCode.VcF3 => GlobalKeyCode.F3,
            KeyCode.VcF4 => GlobalKeyCode.F4,
            KeyCode.VcF5 => GlobalKeyCode.F5,
            KeyCode.VcF6 => GlobalKeyCode.F6,
            KeyCode.VcF7 => GlobalKeyCode.F7,
            KeyCode.VcF8 => GlobalKeyCode.F8,
            KeyCode.VcF9 => GlobalKeyCode.F9,
            KeyCode.VcF10 => GlobalKeyCode.F10,
            KeyCode.VcF11 => GlobalKeyCode.F11,
            KeyCode.VcF12 => GlobalKeyCode.F12,

            // Extended function keys F13-F24
            KeyCode.VcF13 => GlobalKeyCode.F13,
            KeyCode.VcF14 => GlobalKeyCode.F14,
            KeyCode.VcF15 => GlobalKeyCode.F15,
            KeyCode.VcF16 => GlobalKeyCode.F16,
            KeyCode.VcF17 => GlobalKeyCode.F17,
            KeyCode.VcF18 => GlobalKeyCode.F18,
            KeyCode.VcF19 => GlobalKeyCode.F19,
            KeyCode.VcF20 => GlobalKeyCode.F20,
            KeyCode.VcF21 => GlobalKeyCode.F21,
            KeyCode.VcF22 => GlobalKeyCode.F22,
            KeyCode.VcF23 => GlobalKeyCode.F23,
            KeyCode.VcF24 => GlobalKeyCode.F24,

            // Lock keys
            KeyCode.VcCapsLock => GlobalKeyCode.CapsLock,
            KeyCode.VcScrollLock => GlobalKeyCode.ScrollLock,
            KeyCode.VcNumLock => GlobalKeyCode.NumLock,

            // Other keys
            KeyCode.VcPause => GlobalKeyCode.Pause,
            KeyCode.VcPrintScreen => GlobalKeyCode.PrintScreen,
            KeyCode.VcInsert => GlobalKeyCode.Insert,
            KeyCode.VcDelete => GlobalKeyCode.Delete,
            KeyCode.VcHome => GlobalKeyCode.Home,
            KeyCode.VcEnd => GlobalKeyCode.End,
            KeyCode.VcPageUp => GlobalKeyCode.PageUp,
            KeyCode.VcPageDown => GlobalKeyCode.PageDown,

            _ => GlobalKeyCode.Unknown
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Uninstall();
        _disposed = true;
    }
}
