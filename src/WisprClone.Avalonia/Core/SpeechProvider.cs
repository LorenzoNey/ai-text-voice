namespace WisprClone.Core;

/// <summary>
/// Available speech recognition providers.
/// </summary>
public enum SpeechProvider
{
    /// <summary>
    /// Offline recognition using Windows System.Speech.
    /// Only available on Windows.
    /// </summary>
    Offline,

    /// <summary>
    /// Cloud recognition using Azure Cognitive Services.
    /// </summary>
    Azure,

    /// <summary>
    /// Cloud recognition using OpenAI Whisper API.
    /// </summary>
    OpenAI
}
