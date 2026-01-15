using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace WisprClone.Infrastructure;

/// <summary>
/// Provides a low-level keyboard hook for capturing global keyboard events.
/// </summary>
public class LowLevelKeyboardHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private bool _disposed;

    /// <summary>
    /// Raised when a key is pressed.
    /// </summary>
    public event EventHandler<KeyEventArgs>? KeyDown;

    /// <summary>
    /// Raised when a key is released.
    /// </summary>
    public event EventHandler<KeyEventArgs>? KeyUp;

    public LowLevelKeyboardHook()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// Installs the keyboard hook.
    /// </summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero)
            return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetModuleHandle(curModule?.ModuleName),
            0);

        if (_hookId == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to install keyboard hook. Error code: {error}");
        }
    }

    /// <summary>
    /// Uninstalls the keyboard hook.
    /// </summary>
    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);

            var wparam = wParam.ToInt32();
            if (wparam == NativeMethods.WM_KEYDOWN || wparam == NativeMethods.WM_SYSKEYDOWN)
            {
                KeyDown?.Invoke(this, new KeyEventArgs(key));
            }
            else if (wparam == NativeMethods.WM_KEYUP || wparam == NativeMethods.WM_SYSKEYUP)
            {
                KeyUp?.Invoke(this, new KeyEventArgs(key));
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Uninstall();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~LowLevelKeyboardHook()
    {
        Dispose();
    }
}

/// <summary>
/// Event arguments for keyboard events.
/// </summary>
public class KeyEventArgs : EventArgs
{
    public Key Key { get; }

    public KeyEventArgs(Key key)
    {
        Key = key;
    }
}
