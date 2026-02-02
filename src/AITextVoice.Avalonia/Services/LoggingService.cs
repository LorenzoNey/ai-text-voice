using AITextVoice.Core;
using AITextVoice.Services.Interfaces;

namespace AITextVoice.Services;

/// <summary>
/// Centralized logging service that respects the EnableLogging setting.
/// All log entries are written to a single consolidated log file.
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly ISettingsService _settingsService;
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public LoggingService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _logDirectory = Path.Combine(appData, Constants.AppName, "logs");
    }

    /// <inheritdoc />
    public void Log(string source, string message)
    {
        if (!_settingsService.Current.EnableLogging)
            return;

        WriteLog(source, message);
    }

    /// <inheritdoc />
    public void Log(string message)
    {
        Log("General", message);
    }

    private void WriteLog(string source, string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_logDirectory);

                var logFileName = $"wispr_{DateTime.Now:yyyy-MM-dd}.log";
                var logPath = Path.Combine(_logDirectory, logFileName);

                var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{source}] {message}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Silently ignore logging errors to prevent cascading failures
        }
    }
}
