using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class CoalescionProfileSettingsView : UserControl
{
    public CoalescionProfileSettingsView()
    {
        InitializeComponent();
        SetupControls();
    }

    private void SetupControls()
    {
        // Create ContentControls for the bound properties
        var inputSmoothingControl = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        inputSmoothingControl.Bind(ContentControl.ContentProperty, new Binding("InputSmoothingHalfLife"));

        var scaleSmoothingControl = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        scaleSmoothingControl.Bind(ContentControl.ContentProperty, new Binding("ScaleSmoothingHalfLife"));

        // Create the DualColumnLabelField with both fields
        var labelField = new DualColumnLabelField(
            ("Input Smoothing Half Life", inputSmoothingControl),
            ("Scale Smoothing Half Life", scaleSmoothingControl)
        );

        // Add it to the main StackPanel
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }
}
