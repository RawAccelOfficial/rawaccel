using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;
using userinterface.ViewModels;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    public SettingsPageViewModel()
    {
    }

    private SettingsService SettingsService =>
        App.Services!.GetRequiredService<SettingsService>();

    public bool ShowToastNotifications
    {
        get => SettingsService.ShowToastNotifications;
        set
        {
            SettingsService.ShowToastNotifications = value;
            OnPropertyChanged();
        }
    }
}