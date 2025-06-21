using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DeviceGroupsView : UserControl
{
    public DeviceGroupsView()
    {
        InitializeComponent();
    }

    public void AddDeviceGroup(object sender, RoutedEventArgs args)
    {
        if (DataContext is DeviceGroupsViewModel viewModel)
        {
            _ = viewModel.TryAddNewDeviceGroup();
        }
    }
}