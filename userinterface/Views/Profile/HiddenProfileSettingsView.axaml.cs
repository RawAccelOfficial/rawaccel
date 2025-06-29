using Avalonia.Controls;
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
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_hiddenSettingsField == null)
        {
            SetupHiddenSettingsFields();
        }
    }

    private void SetupHiddenSettingsFields()
    {
        if (DataContext is not HiddenProfileSettingsViewModel viewModel)
            return;

        _hiddenSettingsField = new DualColumnLabelField(
            ("Rotation", CreateInputControl(viewModel.RotationField)),
            ("LR Ratio", CreateInputControl(viewModel.LRRatioField)),
            ("UD Ratio", CreateInputControl(viewModel.UDRatioField)),
            ("Speed Cap", CreateInputControl(viewModel.SpeedCapField)),
            ("Angle Snapping", CreateInputControl(viewModel.AngleSnappingField)),
            ("Output Smoothing Half Life", CreateInputControl(viewModel.OutputSmoothingHalfLifeField))
        );

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(_hiddenSettingsField);
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
