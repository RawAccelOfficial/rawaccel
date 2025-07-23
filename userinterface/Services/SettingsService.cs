using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace userinterface.Services;

public class SettingsService : ISettingsService
{
    private readonly string settingsFilePath;
    private bool showToastNotifications = true;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "RawAccel");
        Directory.CreateDirectory(appFolder);
        settingsFilePath = Path.Combine(appFolder, "settings.json");

        Load();
    }

    public bool ShowToastNotifications
    {
        get => showToastNotifications;
        set
        {
            if (showToastNotifications != value)
            {
                showToastNotifications = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool TrySave(out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var settings = new { ShowToastNotifications = showToastNotifications };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save settings: {ex.Message}";
            Console.WriteLine(errorMessage);
            return false;
        }
    }

    public void Save()
    {
        TrySave(out _);
    }

    public bool TryLoad(out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            if (File.Exists(settingsFilePath))
            {
                var json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize<JsonElement>(json);

                if (settings.TryGetProperty("ShowToastNotifications", out var showToastProp))
                {
                    showToastNotifications = showToastProp.GetBoolean();
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load settings: {ex.Message}";
            Console.WriteLine(errorMessage);
            showToastNotifications = true;
            return false;
        }
    }

    public void Load()
    {
        TryLoad(out _);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
