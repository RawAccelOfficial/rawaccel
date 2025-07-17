using Avalonia.Controls;
using userinterface.ViewModels.Settings;

namespace userinterface.Views.Settings;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
}