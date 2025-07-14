using System.ComponentModel;

namespace userinterface.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    bool ShowToastNotifications { get; set; }

    void Save();
    void Load();
}
