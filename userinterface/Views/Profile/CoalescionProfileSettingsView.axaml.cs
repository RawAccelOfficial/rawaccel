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
        var inputSmoothingControl = CreateSmoothingControl("InputSmoothingHalfLife");
        var scaleSmoothingControl = CreateSmoothingControl("ScaleSmoothingHalfLife");

        var fieldViewModel = new DualColumnLabelFieldViewModel();
        fieldViewModel.AddField("Input Smoothing Half Life", inputSmoothingControl);
        fieldViewModel.AddField("Scale Smoothing Half Life", scaleSmoothingControl);

        var labelField = new DualColumnLabelFieldView(fieldViewModel);
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }

    private ContentControl CreateSmoothingControl(string bindingPath)
    {
        var control = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        control.Bind(ContentControl.ContentProperty, new Binding(bindingPath));
        return control;
    }
}
