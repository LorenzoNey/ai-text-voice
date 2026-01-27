using System;

namespace WisprClone.Infrastructure.Keyboard;

/// <summary>
/// Common interface for all hotkey detection strategies.
/// </summary>
public interface IHotkeyDetector : IDisposable
{
    /// <summary>
    /// Raised when the hotkey is activated.
    /// </summary>
    event EventHandler? HotkeyActivated;

    /// <summary>
    /// Starts listening for hotkey events.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops listening for hotkey events.
    /// </summary>
    void Stop();

    /// <summary>
    /// Whether the detector is currently active and listening.
    /// </summary>
    bool IsActive { get; }
}
