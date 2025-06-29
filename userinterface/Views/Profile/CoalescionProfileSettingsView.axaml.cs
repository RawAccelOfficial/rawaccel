using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
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

        // Create ViewModel and add fields
        var fieldViewModel = new DualColumnLabelFieldViewModel();
        fieldViewModel.AddField("Input Smoothing Half Life", inputSmoothingControl);
        fieldViewModel.AddField("Scale Smoothing Half Life", scaleSmoothingControl);

        var labelField = new DualColumnLabelFieldView(fieldViewModel);

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }
}
