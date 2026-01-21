using System;
using SharpHook;
using SharpHook.Native;

namespace WisprClone.Infrastructure.Keyboard;

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
            KeyCode.VcLeftControl => GlobalKeyCode.LeftCtrl,
            KeyCode.VcRightControl => GlobalKeyCode.RightCtrl,
            KeyCode.VcLeftAlt => GlobalKeyCode.LeftAlt,
            KeyCode.VcRightAlt => GlobalKeyCode.RightAlt,
            KeyCode.VcLeftShift => GlobalKeyCode.LeftShift,
            KeyCode.VcRightShift => GlobalKeyCode.RightShift,
            KeyCode.VcLeftMeta => GlobalKeyCode.LeftMeta,   // Command on macOS
            KeyCode.VcRightMeta => GlobalKeyCode.RightMeta, // Command on macOS
            KeyCode.VcSpace => GlobalKeyCode.Space,
            KeyCode.VcEnter => GlobalKeyCode.Enter,
            KeyCode.VcEscape => GlobalKeyCode.Escape,
            KeyCode.VcTab => GlobalKeyCode.Tab,
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
