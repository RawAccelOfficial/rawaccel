using Avalonia.Controls;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class HiddenProfileSettingsView : UserControl
{
    public HiddenProfileSettingsView()
    {
        InitializeComponent();

        // Add all the additional fields once the component is initialized
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Only add fields once when loaded
        if (HiddenSettingsField.AdditionalFields.Count == 0)
        {
            AddHiddenSettingsFields();
        }
    }

    private void AddHiddenSettingsFields()
    {
        if (DataContext is not HiddenProfileSettingsViewModel viewModel)
            return;

        // Add all the hidden settings fields
        HiddenSettingsField.AddField("LR Ratio", CreateInputControl(viewModel.LRRatioField));
        HiddenSettingsField.AddField("UD Ratio", CreateInputControl(viewModel.UDRatioField));
        HiddenSettingsField.AddField("Speed Cap", CreateInputControl(viewModel.SpeedCapField));
        HiddenSettingsField.AddField("Angle Snapping", CreateInputControl(viewModel.AngleSnappingField));
        HiddenSettingsField.AddField("Output Smoothing Half Life", CreateInputControl(viewModel.OutputSmoothingHalfLifeField));
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
