using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using userspace_backend;

namespace userinterface.Services;

public class SettingsService : ISettingsService
{
    private readonly BackEnd backEnd;

    public SettingsService(BackEnd backEnd)
    {
        this.backEnd = backEnd;
        
        // Subscribe to backend settings property changes
        if (this.backEnd.Settings != null)
        {
            this.backEnd.Settings.PropertyChanged += OnBackEndSettingsPropertyChanged;
        }
    }

    private void OnBackEndSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward property change notifications
        OnPropertyChanged(e.PropertyName);
    }

    public bool ShowToastNotifications
    {
        get => backEnd.Settings?.ShowToastNotifications ?? true;
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.ShowToastNotifications != value)
            {
                backEnd.Settings.ShowToastNotifications = value;
                Save();
            }
        }
    }

    public string Theme
    {
        get => backEnd.Settings?.Theme ?? "System";
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.Theme != value)
            {
                backEnd.Settings.Theme = value;
                Save();
            }
        }
    }

    public bool AutoSaveProfiles
    {
        get => backEnd.Settings?.AutoSaveProfiles ?? true;
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.AutoSaveProfiles != value)
            {
                backEnd.Settings.AutoSaveProfiles = value;
                Save();
            }
        }
    }

    public int SaveIntervalMinutes
    {
        get => backEnd.Settings?.SaveIntervalMinutes ?? 5;
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.SaveIntervalMinutes != value)
            {
                backEnd.Settings.SaveIntervalMinutes = value;
                Save();
            }
        }
    }

    public bool EnableLogging
    {
        get => backEnd.Settings?.EnableLogging ?? false;
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.EnableLogging != value)
            {
                backEnd.Settings.EnableLogging = value;
                Save();
            }
        }
    }

    public string LogLevel
    {
        get => backEnd.Settings?.LogLevel ?? "Info";
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.LogLevel != value)
            {
                backEnd.Settings.LogLevel = value;
                Save();
            }
        }
    }

    public bool CheckForUpdates
    {
        get => backEnd.Settings?.CheckForUpdates ?? true;
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.CheckForUpdates != value)
            {
                backEnd.Settings.CheckForUpdates = value;
                Save();
            }
        }
    }

    public string Language
    {
        get => backEnd.Settings?.Language ?? "en-US";
        set
        {
            if (backEnd.Settings != null && backEnd.Settings.Language != value)
            {
                backEnd.Settings.Language = value;
                Save();
            }
        }
    }

    public bool TrySave(out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            backEnd.Apply();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save settings: {ex.Message}";
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
            backEnd.Load();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load settings: {ex.Message}";
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
