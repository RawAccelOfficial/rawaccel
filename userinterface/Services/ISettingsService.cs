using System.ComponentModel;

namespace userinterface.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    bool ShowToastNotifications { get; set; }
    string Theme { get; set; }
    bool AutoSaveProfiles { get; set; }
    int SaveIntervalMinutes { get; set; }
    bool EnableLogging { get; set; }
    string LogLevel { get; set; }
    bool CheckForUpdates { get; set; }
    string Language { get; set; }

    bool TrySave(out string? errorMessage);
    bool TryLoad(out string? errorMessage);
    void Save();
    void Load();
}