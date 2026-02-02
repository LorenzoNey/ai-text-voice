namespace AITextVoice.Services.Interfaces;

/// <summary>
/// Centralized logging service that respects the EnableLogging setting.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs a message with the specified source tag.
    /// Only writes if EnableLogging is true in settings.
    /// </summary>
    /// <param name="source">The source component (e.g., "App", "Azure", "Whisper")</param>
    /// <param name="message">The message to log</param>
    void Log(string source, string message);

    /// <summary>
    /// Logs a message. Source will be inferred or set to "General".
    /// Only writes if EnableLogging is true in settings.
    /// </summary>
    /// <param name="message">The message to log</param>
    void Log(string message);
}
