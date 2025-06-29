using Avalonia.Controls;
using userinterface.Views.Controls;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Profile;

public partial class AccelerationLUTSettingsView : UserControl
{
    public AccelerationLUTSettingsView()
    {
        InitializeComponent();
        SetupControls();
    }

    private void SetupControls()
    {
        var applyAsComboBox = new ComboBox
        {
            Items =
            {
                new ComboBoxItem { Content = "Velocity" },
                new ComboBoxItem { Content = "Sensitivity" }
            },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        var viewModel = new DualColumnLabelFieldViewModel();
        viewModel.AddField("Apply as:", applyAsComboBox);

        var labelField = new DualColumnLabelFieldView(viewModel);

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }
}
