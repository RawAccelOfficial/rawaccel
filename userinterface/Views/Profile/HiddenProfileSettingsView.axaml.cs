using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class HiddenProfileSettingsView : UserControl
{
    private DualColumnLabelField? _hiddenSettingsField;

    public HiddenProfileSettingsView()
    {
        InitializeComponent();
        // Add all the fields once the component is initialized
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Only add fields once when loaded
        if (_hiddenSettingsField == null)
        {
            SetupHiddenSettingsFields();
        }
    }

    private void SetupHiddenSettingsFields()
    {
        if (DataContext is not HiddenProfileSettingsViewModel viewModel)
            return;

        // Create the rotation field control with an actual input control
        // If RotationField is a bound object, use ContentControl, otherwise create a proper input
        var rotationControl = CreateInputControlForField("RotationField");

        // Create the DualColumnLabelField with the rotation field first
        _hiddenSettingsField = new DualColumnLabelField(
            ("Rotation", rotationControl)
        );

        // Add all the additional hidden settings fields
        _hiddenSettingsField.AddField("LR Ratio", CreateInputControl(viewModel.LRRatioField));
        _hiddenSettingsField.AddField("UD Ratio", CreateInputControl(viewModel.UDRatioField));
        _hiddenSettingsField.AddField("Speed Cap", CreateInputControl(viewModel.SpeedCapField));
        _hiddenSettingsField.AddField("Angle Snapping", CreateInputControl(viewModel.AngleSnappingField));
        _hiddenSettingsField.AddField("Output Smoothing Half Life", CreateInputControl(viewModel.OutputSmoothingHalfLifeField));

        // Add it to the main StackPanel
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(_hiddenSettingsField);
    }

    private Control CreateInputControlForField(string bindingPath)
    {
        // Create a proper input control for the rotation field
        // You can change this to NumericUpDown if it's a numeric field
        var textBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Bind to the field - you might need to adjust the binding path
        // For example, if RotationField has a Value property: "RotationField.Value"
        textBox.Bind(TextBox.TextProperty, new Binding(bindingPath));

        return textBox;
    }

    private Control CreateInputControl(object bindingSource)
    {
        return new ContentControl
        {
            Content = bindingSource,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
