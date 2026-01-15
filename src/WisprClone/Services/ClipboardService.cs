using System.Windows;
using WisprClone.Services.Interfaces;

namespace WisprClone.Services;

/// <summary>
/// Service for clipboard operations.
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <inheritdoc />
    public Task SetTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Task.CompletedTask;

        // Clipboard operations must be on STA thread
        var tcs = new TaskCompletionSource<bool>();

        // Use a dedicated STA thread for clipboard operations
        var thread = new Thread(() =>
        {
            try
            {
                // Retry logic for clipboard access
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Clipboard.SetDataObject(text, true); // true = persist after app exits
                        tcs.SetResult(true);
                        return;
                    }
                    catch (Exception)
                    {
                        if (i < 4)
                            Thread.Sleep(50);
                    }
                }
                tcs.SetResult(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
                tcs.SetResult(false);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    /// <inheritdoc />
    public Task<string> GetTextAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var thread = new Thread(() =>
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    tcs.SetResult(Clipboard.GetText());
                }
                else
                {
                    tcs.SetResult(string.Empty);
                }
            }
            catch
            {
                tcs.SetResult(string.Empty);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }
}
