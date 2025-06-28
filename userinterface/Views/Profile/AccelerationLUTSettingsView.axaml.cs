using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using userinterface.Views.Controls;

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
                new ComboBoxItem { Content = new TextBlock { Text = "Velocity" } },
                new ComboBoxItem { Content = new TextBlock { Text = "Sensitivity" } }
            }
        };

        var labelField = new DualColumnLabelField(
            ("Apply as:", applyAsComboBox)
        );

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelField);
    }
}
