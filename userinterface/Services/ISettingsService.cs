using System.ComponentModel;

namespace userinterface.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    bool ShowToastNotifications { get; set; }
    string Theme { get; set; }
    bool ShowConfirmModals { get; set; }
    string Language { get; set; }

    bool TrySave(out string? errorMessage);
    bool TryLoad(out string? errorMessage);
    void Save();
    void Load();
}