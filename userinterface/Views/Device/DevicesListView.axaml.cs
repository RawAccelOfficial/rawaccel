using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DevicesListView : UserControl
{
    public DevicesListView()
    {
        InitializeComponent();
    }

    public void AddDevice(object sender, RoutedEventArgs args)
    {
        if (DataContext is DevicesListViewModel viewModel)
        {
            _ = viewModel.TryAddDevice();
        }
    }
}