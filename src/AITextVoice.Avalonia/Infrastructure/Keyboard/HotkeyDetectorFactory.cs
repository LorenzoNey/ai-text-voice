using System;
using AITextVoice.Models;

namespace AITextVoice.Infrastructure.Keyboard;

/// <summary>
/// Factory for creating hotkey detectors from configuration.
/// </summary>
public static class HotkeyDetectorFactory
{
    /// <summary>
    /// Creates a hotkey detector based on the provided configuration.
    /// </summary>
    /// <param name="config">The hotkey configuration.</param>
    /// <param name="hook">The global keyboard hook to use.</param>
    /// <param name="logAction">Optional logging action for debugging.</param>
    /// <returns>An IHotkeyDetector instance appropriate for the configuration.</returns>
    public static IHotkeyDetector Create(
        HotkeyConfiguration config,
        IGlobalKeyboardHook hook,
        Action<string>? logAction = null)
    {
        var keyCode = ParseKeyCode(config.PrimaryKey);

        return config.ActivationType switch
        {
            HotkeyActivationType.DoubleTap => new DoubleKeyTapDetector(
                hook,
                keyCode,
                config.DoubleTapIntervalMs,
                config.MaxTapHoldDurationMs,
                logAction),

            HotkeyActivationType.SinglePress => new SingleKeyPressDetector(
                hook,
                keyCode,
                logAction),

            HotkeyActivationType.Hold => new KeyHoldDetector(
                hook,
                keyCode,
                config.HoldDurationMs,
                logAction),

            HotkeyActivationType.Combination => new ModifierCombinationDetector(
                hook,
                keyCode,
                config.Modifiers ?? "Ctrl",
                logAction),

            _ => throw new ArgumentException($"Unknown activation type: {config.ActivationType}")
        };
    }

    /// <summary>
    /// Parses a key name string to GlobalKeyCode.
    /// </summary>
    public static GlobalKeyCode ParseKeyCode(string keyName)
    {
        return keyName.ToUpperInvariant() switch
        {
            // Modifier keys
            "CTRL" or "CONTROL" or "LEFTCTRL" => GlobalKeyCode.LeftCtrl,
            "RIGHTCTRL" => GlobalKeyCode.RightCtrl,
            "SHIFT" or "LEFTSHIFT" => GlobalKeyCode.LeftShift,
            "RIGHTSHIFT" => GlobalKeyCode.RightShift,
            "ALT" or "LEFTALT" or "OPTION" => GlobalKeyCode.LeftAlt,
            "RIGHTALT" => GlobalKeyCode.RightAlt,
            "META" or "WIN" or "WINDOWS" or "CMD" or "COMMAND" => GlobalKeyCode.LeftMeta,

            // Common keys
            "SPACE" => GlobalKeyCode.Space,
            "ENTER" or "RETURN" => GlobalKeyCode.Enter,
            "ESC" or "ESCAPE" => GlobalKeyCode.Escape,
            "TAB" => GlobalKeyCode.Tab,

            // Function keys F1-F12
            "F1" => GlobalKeyCode.F1,
            "F2" => GlobalKeyCode.F2,
            "F3" => GlobalKeyCode.F3,
            "F4" => GlobalKeyCode.F4,
            "F5" => GlobalKeyCode.F5,
            "F6" => GlobalKeyCode.F6,
            "F7" => GlobalKeyCode.F7,
            "F8" => GlobalKeyCode.F8,
            "F9" => GlobalKeyCode.F9,
            "F10" => GlobalKeyCode.F10,
            "F11" => GlobalKeyCode.F11,
            "F12" => GlobalKeyCode.F12,

            // Extended function keys F13-F24
            "F13" => GlobalKeyCode.F13,
            "F14" => GlobalKeyCode.F14,
            "F15" => GlobalKeyCode.F15,
            "F16" => GlobalKeyCode.F16,
            "F17" => GlobalKeyCode.F17,
            "F18" => GlobalKeyCode.F18,
            "F19" => GlobalKeyCode.F19,
            "F20" => GlobalKeyCode.F20,
            "F21" => GlobalKeyCode.F21,
            "F22" => GlobalKeyCode.F22,
            "F23" => GlobalKeyCode.F23,
            "F24" => GlobalKeyCode.F24,

            // Lock keys
            "CAPSLOCK" => GlobalKeyCode.CapsLock,
            "SCROLLLOCK" => GlobalKeyCode.ScrollLock,
            "NUMLOCK" => GlobalKeyCode.NumLock,

            // Other keys
            "PAUSE" => GlobalKeyCode.Pause,
            "PRINTSCREEN" or "PRTSC" => GlobalKeyCode.PrintScreen,
            "INSERT" or "INS" => GlobalKeyCode.Insert,
            "DELETE" or "DEL" => GlobalKeyCode.Delete,
            "HOME" => GlobalKeyCode.Home,
            "END" => GlobalKeyCode.End,
            "PAGEUP" or "PGUP" => GlobalKeyCode.PageUp,
            "PAGEDOWN" or "PGDN" => GlobalKeyCode.PageDown,

            // Default fallback to Ctrl
            _ => GlobalKeyCode.LeftCtrl
        };
    }

    /// <summary>
    /// Gets the display-friendly name for a key code.
    /// </summary>
    public static string GetKeyDisplayName(GlobalKeyCode keyCode)
    {
        return keyCode switch
        {
            GlobalKeyCode.LeftCtrl or GlobalKeyCode.RightCtrl => "Ctrl",
            GlobalKeyCode.LeftShift or GlobalKeyCode.RightShift => "Shift",
            GlobalKeyCode.LeftAlt or GlobalKeyCode.RightAlt => "Alt",
            GlobalKeyCode.LeftMeta or GlobalKeyCode.RightMeta => OperatingSystem.IsMacOS() ? "Cmd" : "Win",
            _ => keyCode.ToString()
        };
    }
}
