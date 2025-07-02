using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
    }

    public void DeleteSelf(object sender, RoutedEventArgs args)
    {
        if (DataContext is DeviceViewModel viewModel)
        {
            viewModel.DeleteSelf();
        }
    }
}