using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class HiddenProfileSettingsView : UserControl
{
    private DualColumnLabelFieldView? _hiddenSettingsField;

    public HiddenProfileSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
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

        var hiddenSettingsFieldViewModel = new DualColumnLabelFieldViewModel();
        hiddenSettingsFieldViewModel.AddField("Rotation", CreateInputControl(viewModel.RotationField));
        hiddenSettingsFieldViewModel.AddField("LR Ratio", CreateInputControl(viewModel.LRRatioField));
        hiddenSettingsFieldViewModel.AddField("UD Ratio", CreateInputControl(viewModel.UDRatioField));
        hiddenSettingsFieldViewModel.AddField("Speed Cap", CreateInputControl(viewModel.SpeedCapField));
        hiddenSettingsFieldViewModel.AddField("Angle Snapping", CreateInputControl(viewModel.AngleSnappingField));
        hiddenSettingsFieldViewModel.AddField("Output Smoothing Half Life", CreateInputControl(viewModel.OutputSmoothingHalfLifeField));

        _hiddenSettingsField = new DualColumnLabelFieldView(hiddenSettingsFieldViewModel);

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
