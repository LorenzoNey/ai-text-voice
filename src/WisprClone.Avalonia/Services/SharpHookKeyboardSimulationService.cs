using SharpHook;
using SharpHook.Native;
using WisprClone.Services.Interfaces;
using System.IO;

namespace WisprClone.Services;

/// <summary>
/// Keyboard simulation service using SharpHook's EventSimulator.
/// Cross-platform support for Windows, macOS, and Linux.
/// </summary>
public class SharpHookKeyboardSimulationService : IKeyboardSimulationService
{
    private readonly ISettingsService _settingsService;
    private readonly EventSimulator _simulator;
    private bool _disposed;

    public SharpHookKeyboardSimulationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _simulator = new EventSimulator();
    }

    public bool IsAvailable => true;

    public async Task<bool> SimulatePasteAsync()
    {
        try
        {
            Log("SimulatePasteAsync called");

            // Wait longer to ensure user has fully released the hotkey (Ctrl)
            // and clipboard is ready
            await Task.Delay(250);

            // Determine modifier key based on platform
            // macOS uses Cmd (Meta), Windows/Linux use Ctrl
            var modifierKey = OperatingSystem.IsMacOS()
                ? KeyCode.VcLeftMeta
                : KeyCode.VcLeftControl;

            Log($"Simulating paste with modifier: {modifierKey}");

            // Simulate paste: Modifier down, V down, V up, Modifier up
            var result = _simulator.SimulateKeyPress(modifierKey);
            Log($"Ctrl press result: {result}");
            await Task.Delay(20); // Small delay between key events

            result = _simulator.SimulateKeyPress(KeyCode.VcV);
            Log($"V press result: {result}");
            await Task.Delay(20);

            result = _simulator.SimulateKeyRelease(KeyCode.VcV);
            Log($"V release result: {result}");
            await Task.Delay(20);

            result = _simulator.SimulateKeyRelease(modifierKey);
            Log($"Ctrl release result: {result}");

            Log("SimulatePasteAsync completed");
            return true;
        }
        catch (Exception ex)
        {
            Log($"SimulatePasteAsync error: {ex.Message}");
            return false;
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "wispr_log.txt");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [KeyboardSim] {message}";
        try { File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // EventSimulator does not require disposal
        _disposed = true;
    }
}
