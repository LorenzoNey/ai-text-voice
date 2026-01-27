namespace WisprClone.Models;

/// <summary>
/// Type of hotkey activation.
/// </summary>
public enum HotkeyActivationType
{
    /// <summary>
    /// Double-tap a key (e.g., Ctrl+Ctrl).
    /// </summary>
    DoubleTap,

    /// <summary>
    /// Single key press (e.g., F9).
    /// </summary>
    SinglePress,

    /// <summary>
    /// Hold a key for a duration (e.g., hold Ctrl for 1 second).
    /// </summary>
    Hold,

    /// <summary>
    /// Modifier combination (e.g., Ctrl+Space).
    /// </summary>
    Combination
}

/// <summary>
/// Configuration for a hotkey activation pattern.
/// </summary>
public class HotkeyConfiguration
{
    /// <summary>
    /// The type of activation (DoubleTap, SinglePress, Hold, Combination).
    /// </summary>
    public HotkeyActivationType ActivationType { get; set; } = HotkeyActivationType.DoubleTap;

    /// <summary>
    /// The primary key (e.g., "Ctrl", "F9", "Space").
    /// </summary>
    public string PrimaryKey { get; set; } = "Ctrl";

    /// <summary>
    /// For Combination type: the required modifiers (e.g., "Ctrl", "Ctrl+Shift").
    /// </summary>
    public string? Modifiers { get; set; }

    /// <summary>
    /// Maximum interval between taps for DoubleTap (milliseconds).
    /// </summary>
    public int DoubleTapIntervalMs { get; set; } = 400;

    /// <summary>
    /// Maximum hold duration to count as a tap for DoubleTap (milliseconds).
    /// </summary>
    public int MaxTapHoldDurationMs { get; set; } = 200;

    /// <summary>
    /// Duration to hold for Hold activation type (milliseconds).
    /// </summary>
    public int HoldDurationMs { get; set; } = 1000;

    /// <summary>
    /// Creates a default double-tap Ctrl configuration.
    /// </summary>
    public static HotkeyConfiguration DefaultStt => new()
    {
        ActivationType = HotkeyActivationType.DoubleTap,
        PrimaryKey = "Ctrl",
        DoubleTapIntervalMs = 400,
        MaxTapHoldDurationMs = 200
    };

    /// <summary>
    /// Creates a default double-tap Shift configuration.
    /// </summary>
    public static HotkeyConfiguration DefaultTts => new()
    {
        ActivationType = HotkeyActivationType.DoubleTap,
        PrimaryKey = "Shift",
        DoubleTapIntervalMs = 400,
        MaxTapHoldDurationMs = 200
    };

    /// <summary>
    /// Serializes the configuration to a string format.
    /// Format: "ActivationType:PrimaryKey:Modifiers:DoubleTapIntervalMs:MaxTapHoldDurationMs:HoldDurationMs"
    /// </summary>
    public string Serialize()
    {
        return $"{ActivationType}:{PrimaryKey}:{Modifiers ?? ""}:{DoubleTapIntervalMs}:{MaxTapHoldDurationMs}:{HoldDurationMs}";
    }

    /// <summary>
    /// Deserializes a configuration from a string.
    /// Handles both new format and legacy format ("Ctrl", "Shift", "Alt").
    /// </summary>
    public static HotkeyConfiguration Deserialize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultStt;

        // Handle legacy format (simple key names like "Ctrl", "Shift", "Alt")
        if (!value.Contains(':'))
        {
            return new HotkeyConfiguration
            {
                ActivationType = HotkeyActivationType.DoubleTap,
                PrimaryKey = value,
                DoubleTapIntervalMs = 400,
                MaxTapHoldDurationMs = 200
            };
        }

        var parts = value.Split(':');
        if (parts.Length < 2)
            return DefaultStt;

        var config = new HotkeyConfiguration();

        // Parse activation type
        if (Enum.TryParse<HotkeyActivationType>(parts[0], out var activationType))
            config.ActivationType = activationType;

        // Parse primary key
        config.PrimaryKey = parts[1];

        // Parse modifiers (may be empty)
        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
            config.Modifiers = parts[2];

        // Parse timing values
        if (parts.Length > 3 && int.TryParse(parts[3], out var doubleTapInterval))
            config.DoubleTapIntervalMs = doubleTapInterval;

        if (parts.Length > 4 && int.TryParse(parts[4], out var maxTapHold))
            config.MaxTapHoldDurationMs = maxTapHold;

        if (parts.Length > 5 && int.TryParse(parts[5], out var holdDuration))
            config.HoldDurationMs = holdDuration;

        return config;
    }

    /// <summary>
    /// Gets a human-readable description of this hotkey configuration.
    /// </summary>
    public string GetDisplayName()
    {
        return ActivationType switch
        {
            HotkeyActivationType.DoubleTap => $"{PrimaryKey} + {PrimaryKey} (double-tap)",
            HotkeyActivationType.SinglePress => $"{PrimaryKey} (single press)",
            HotkeyActivationType.Hold => $"Hold {PrimaryKey} ({HoldDurationMs / 1000.0:F1}s)",
            HotkeyActivationType.Combination => $"{Modifiers} + {PrimaryKey}",
            _ => PrimaryKey
        };
    }

    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    public HotkeyConfiguration Clone()
    {
        return new HotkeyConfiguration
        {
            ActivationType = ActivationType,
            PrimaryKey = PrimaryKey,
            Modifiers = Modifiers,
            DoubleTapIntervalMs = DoubleTapIntervalMs,
            MaxTapHoldDurationMs = MaxTapHoldDurationMs,
            HoldDurationMs = HoldDurationMs
        };
    }
}
