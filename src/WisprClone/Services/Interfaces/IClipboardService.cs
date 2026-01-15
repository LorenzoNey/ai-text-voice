namespace WisprClone.Services.Interfaces;

/// <summary>
/// Interface for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Sets text to the clipboard.
    /// </summary>
    /// <param name="text">The text to copy to clipboard.</param>
    Task SetTextAsync(string text);

    /// <summary>
    /// Gets text from the clipboard.
    /// </summary>
    /// <returns>The text from clipboard, or empty string if not available.</returns>
    Task<string> GetTextAsync();
}
