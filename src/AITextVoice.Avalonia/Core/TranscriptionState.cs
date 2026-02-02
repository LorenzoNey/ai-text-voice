namespace AITextVoice.Core;

/// <summary>
/// Represents the current state of the transcription process.
/// </summary>
public enum TranscriptionState
{
    /// <summary>
    /// Ready and waiting for user activation.
    /// </summary>
    Idle,

    /// <summary>
    /// Initializing recognition engine.
    /// </summary>
    Initializing,

    /// <summary>
    /// Actively listening and transcribing.
    /// </summary>
    Listening,

    /// <summary>
    /// Processing final results.
    /// </summary>
    Processing,

    /// <summary>
    /// Copying text to clipboard.
    /// </summary>
    CopyingToClipboard,

    /// <summary>
    /// Error state - can transition back to Idle.
    /// </summary>
    Error
}

/// <summary>
/// Represents the state of the speech recognition engine.
/// </summary>
public enum RecognitionState
{
    Idle,
    Initializing,
    Listening,
    Processing,
    Error
}
