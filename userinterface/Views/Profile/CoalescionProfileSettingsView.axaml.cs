using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
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

        var labelField = new DualColumnLabelField(
            ("Input Smoothing Half Life", inputSmoothingControl),
            ("Scale Smoothing Half Life", scaleSmoothingControl)
        );

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }
}
