using userinterface.Services;
using userinterface.ViewModels;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsService settingsService;

    public SettingsPageViewModel(ISettingsService settingsService)
    {
        this.settingsService = settingsService;
    }

    public bool ShowToastNotifications
    {
        get => settingsService.ShowToastNotifications;
        set
        {
            settingsService.ShowToastNotifications = value;
            OnPropertyChanged();
        }
    }
}