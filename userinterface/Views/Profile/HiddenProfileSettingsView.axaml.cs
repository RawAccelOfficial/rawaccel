using Avalonia.Controls;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class HiddenProfileSettingsView : UserControl
{
    private DualColumnLabelFieldView? _hiddenSettingsField;
    private DualColumnLabelFieldViewModel? _hiddenSettingsFieldViewModel;

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


        _hiddenSettingsFieldViewModel = new DualColumnLabelFieldViewModel();
        _hiddenSettingsFieldViewModel.AddField("Rotation", CreateInputControl(viewModel.RotationField));
        _hiddenSettingsFieldViewModel.AddField("LR Ratio", CreateInputControl(viewModel.LRRatioField));
        _hiddenSettingsFieldViewModel.AddField("UD Ratio", CreateInputControl(viewModel.UDRatioField));
        _hiddenSettingsFieldViewModel.AddField("Speed Cap", CreateInputControl(viewModel.SpeedCapField));
        _hiddenSettingsFieldViewModel.AddField("Angle Snapping", CreateInputControl(viewModel.AngleSnappingField));
        _hiddenSettingsFieldViewModel.AddField("Output Smoothing Half Life", CreateInputControl(viewModel.OutputSmoothingHalfLifeField));

        _hiddenSettingsField = new DualColumnLabelFieldView(_hiddenSettingsFieldViewModel);

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
