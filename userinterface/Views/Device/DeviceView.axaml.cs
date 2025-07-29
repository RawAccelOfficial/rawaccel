using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace userinterface.Views.Device;

public partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
    }

    private async void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[DeviceView] OnDeleteButtonClick called");
        
        // Let the command execute first, then handle propagation
        // The Command binding will execute automatically, we just need to prevent bubbling
        
        // Delay slightly to ensure command executes, then stop propagation
        await Task.Delay(1);
        e.Handled = true;
    }
}