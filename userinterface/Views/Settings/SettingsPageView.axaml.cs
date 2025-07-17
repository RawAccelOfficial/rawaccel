using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using userinterface.ViewModels.Settings;

namespace userinterface.Views.Settings;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
        DataContext = App.Services?.GetRequiredService<SettingsPageViewModel>();
    }
}