namespace WisprClone.Models;

/// <summary>
/// Represents the result of a transcription session.
/// </summary>
public class TranscriptionResult
{
    /// <summary>
    /// The transcribed text.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is a final result.
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// Confidence score (0-1) if available.
    /// </summary>
    public float? Confidence { get; init; }

    /// <summary>
    /// Duration of the transcription session.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// The provider that produced this result.
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the transcription started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Timestamp when the transcription ended.
    /// </summary>
    public DateTime EndTime { get; init; }
}
