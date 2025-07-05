using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class HiddenProfileSettingsView : UserControl
{
    private DualColumnLabelFieldView? HiddenSettingsFieldView;

    public HiddenProfileSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (HiddenSettingsFieldView == null)
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

        HiddenSettingsFieldView = new DualColumnLabelFieldView(hiddenSettingsFieldViewModel);

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(HiddenSettingsFieldView);
    }

    private static ContentControl CreateInputControl(object bindingSource)
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
