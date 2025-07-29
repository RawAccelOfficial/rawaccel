using Avalonia.Controls;
using Avalonia.Interactivity;

namespace userinterface.Views.Device;

public partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
    }

    private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
    {
        // Stop the click event from propagating to parent controls (like EditableExpanderView)
        e.Handled = true;
    }
}