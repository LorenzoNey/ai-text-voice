namespace AITextVoice.Core;

/// <summary>
/// Helper class for getting available speech and TTS providers based on platform.
/// Centralizes provider availability logic for both Settings and Overlay views.
/// </summary>
public static class ProviderHelper
{
    /// <summary>
    /// Gets the list of available speech providers for the current platform.
    /// </summary>
    public static List<(SpeechProvider Provider, string DisplayName, string ShortName)> GetAvailableSpeechProviders()
    {
        var providers = new List<(SpeechProvider Provider, string DisplayName, string ShortName)>();

        // Add platform-specific local speech options first
        if (OperatingSystem.IsWindows())
        {
            providers.Add((SpeechProvider.Offline, "Local (Windows Speech)", "Local (Windows)"));
            providers.Add((SpeechProvider.FasterWhisper, "Faster-Whisper (Offline)", "Faster-Whisper (Offline)"));
            providers.Add((SpeechProvider.WhisperServer, "Whisper Server (Instant)", "Whisper Server"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            providers.Add((SpeechProvider.MacOSNative, "Local (macOS Speech)", "Local (macOS)"));
        }

        // Add cloud providers (available on all platforms)
        providers.Add((SpeechProvider.Azure, "Azure Speech", "Azure"));
        providers.Add((SpeechProvider.OpenAI, "OpenAI Whisper", "OpenAI Whisper"));

        // OpenAI Realtime requires NAudio (Windows only)
        if (OperatingSystem.IsWindows())
        {
            providers.Add((SpeechProvider.OpenAIRealtime, "OpenAI Realtime", "OpenAI Realtime"));
        }

        return providers;
    }

    /// <summary>
    /// Gets the list of available TTS providers for the current platform.
    /// </summary>
    public static List<(TtsProvider Provider, string DisplayName, string ShortName)> GetAvailableTtsProviders()
    {
        var providers = new List<(TtsProvider Provider, string DisplayName, string ShortName)>();

        // Add platform-specific local TTS options first
        if (OperatingSystem.IsWindows())
        {
            providers.Add((TtsProvider.Offline, "Local (Windows Speech)", "Local (Windows)"));
            providers.Add((TtsProvider.Piper, "Piper (Offline)", "Piper (Offline)"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            providers.Add((TtsProvider.MacOSNative, "Local (macOS Speech)", "Local (macOS)"));
        }

        // Add cloud providers (available on all platforms)
        providers.Add((TtsProvider.Azure, "Azure Speech", "Azure"));
        providers.Add((TtsProvider.OpenAI, "OpenAI TTS", "OpenAI"));

        return providers;
    }

    /// <summary>
    /// Gets the provider info text for the current platform.
    /// </summary>
    public static string GetSpeechProviderInfoText()
    {
        if (OperatingSystem.IsMacOS())
            return "Ctrl+Ctrl to dictate. Local uses Apple's on-device recognition.";
        if (OperatingSystem.IsWindows())
            return "Ctrl+Ctrl to dictate. Local uses Windows Speech Recognition.";
        return "Ctrl+Ctrl to dictate. On Linux, only cloud providers are available.";
    }

    /// <summary>
    /// Gets the TTS provider info text for the current platform.
    /// </summary>
    public static string GetTtsProviderInfoText()
    {
        if (OperatingSystem.IsMacOS())
            return "Shift+Shift to read clipboard. Local uses macOS system voices.";
        if (OperatingSystem.IsWindows())
            return "Shift+Shift to read clipboard. Local uses Windows voices.";
        return "Shift+Shift to read clipboard. On Linux, only cloud providers are available.";
    }

    /// <summary>
    /// Gets the default speech provider for the current platform.
    /// </summary>
    public static SpeechProvider GetDefaultSpeechProvider()
    {
        if (OperatingSystem.IsWindows())
            return SpeechProvider.Offline;
        if (OperatingSystem.IsMacOS())
            return SpeechProvider.MacOSNative;
        return SpeechProvider.Azure;
    }

    /// <summary>
    /// Gets the default TTS provider for the current platform.
    /// </summary>
    public static TtsProvider GetDefaultTtsProvider()
    {
        if (OperatingSystem.IsWindows())
            return TtsProvider.Offline;
        if (OperatingSystem.IsMacOS())
            return TtsProvider.MacOSNative;
        return TtsProvider.Azure;
    }
}
