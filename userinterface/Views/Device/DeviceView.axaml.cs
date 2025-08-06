using System.Diagnostics;
using System.Windows.Input;
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

    private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[DeviceView] OnDeleteButtonClick called");
        
        // Stop the event from propagating first
        e.Handled = true;
        
        // Manually execute the delete command
        if (DataContext is DeviceViewModel deviceViewModel)
        {
            Debug.WriteLine("[DeviceView] Executing DeleteCommand manually");
            if (deviceViewModel.DeleteCommand.CanExecute(null))
            {
                deviceViewModel.DeleteCommand.Execute(null);
            }
            else
            {
                Debug.WriteLine("[DeviceView] DeleteCommand cannot execute");
            }
        }
        else
        {
            Debug.WriteLine("[DeviceView] DataContext is not DeviceViewModel");
        }
    }
}