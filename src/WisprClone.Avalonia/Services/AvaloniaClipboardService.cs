using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using WisprClone.Services.Interfaces;

namespace WisprClone.Services;

/// <summary>
/// Cross-platform clipboard service using Avalonia's clipboard API.
/// </summary>
public class AvaloniaClipboardService : IClipboardService
{
    private IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Try MainWindow first
            if (desktop.MainWindow?.Clipboard != null)
            {
                return desktop.MainWindow.Clipboard;
            }

            // Fallback: try to get clipboard from any open window
            foreach (var window in desktop.Windows)
            {
                if (window.Clipboard != null)
                {
                    return window.Clipboard;
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            // Retry logic for clipboard access
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await clipboard.SetTextAsync(text);
                    return;
                }
                catch (Exception)
                {
                    if (i < 4)
                        await Task.Delay(50);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            try
            {
                return await clipboard.GetTextAsync() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }
}
