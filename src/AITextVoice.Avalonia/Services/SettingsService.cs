using System.IO;
using System.Text.Json;
using AITextVoice.Core;
using AITextVoice.Models;
using AITextVoice.Services.Interfaces;

namespace AITextVoice.Services;

/// <summary>
/// Service for managing application settings.
/// Cross-platform compatible using standard .NET APIs.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public AppSettings Current => _settings;

    /// <inheritdoc />
    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, Constants.AppName);
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");

        // Migrate settings from old WisprClone location if needed
        MigrateFromOldLocation(appData, appFolder);

        // Load settings synchronously during construction so they're available immediately
        LoadSync();
    }

    /// <summary>
    /// Migrates settings from the old WisprClone location to the new AITextVoice location.
    /// </summary>
    private void MigrateFromOldLocation(string appData, string newAppFolder)
    {
        try
        {
            // Check if settings already exist in new location
            if (File.Exists(_settingsPath))
            {
                return; // Already have settings, no migration needed
            }

            // Check for old WisprClone settings
            var oldAppFolder = Path.Combine(appData, "WisprClone");
            var oldSettingsPath = Path.Combine(oldAppFolder, "settings.json");

            if (File.Exists(oldSettingsPath))
            {
                // Copy settings file to new location
                File.Copy(oldSettingsPath, _settingsPath, overwrite: false);

                // Also migrate logs folder if it exists
                var oldLogsFolder = Path.Combine(oldAppFolder, "logs");
                var newLogsFolder = Path.Combine(newAppFolder, "logs");
                if (Directory.Exists(oldLogsFolder) && !Directory.Exists(newLogsFolder))
                {
                    Directory.CreateDirectory(newLogsFolder);
                    foreach (var logFile in Directory.GetFiles(oldLogsFolder, "*.log"))
                    {
                        var destFile = Path.Combine(newLogsFolder, Path.GetFileName(logFile));
                        if (!File.Exists(destFile))
                        {
                            File.Copy(logFile, destFile);
                        }
                    }
                }
            }
        }
        catch
        {
            // Migration failed - not critical, will use default settings
        }
    }

    private void LoadSync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    lock (_lock)
                    {
                        _settings = loaded;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If loading fails, use default settings
            _settings = new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json;
            lock (_lock)
            {
                json = JsonSerializer.Serialize(_settings, options);
            }

            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception)
        {
            // Saving failed - could log
        }
    }

    /// <inheritdoc />
    public void Update(Action<AppSettings> updateAction)
    {
        lock (_lock)
        {
            updateAction(_settings);
        }

        SettingsChanged?.Invoke(this, _settings);

        // Fire and forget save
        _ = SaveAsync();
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _settings = new AppSettings();
        }

        SettingsChanged?.Invoke(this, _settings);

        // Fire and forget save
        _ = SaveAsync();
    }
}
