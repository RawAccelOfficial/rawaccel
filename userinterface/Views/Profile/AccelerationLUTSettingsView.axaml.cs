using Avalonia.Controls;
using Avalonia.Layout;
using userinterface.Views.Controls;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Profile;

public partial class AccelerationLUTSettingsView : UserControl
{
    private const string VelocityOptionText = "Velocity";
    private const string SensitivityOptionText = "Sensitivity";
    private const string ApplyAsLabelText = "Apply as:";

    public AccelerationLUTSettingsView()
    {
        InitializeComponent();
        SetupControls();
    }

    private void SetupControls()
    {
        var applyAsComboBox = CreateApplyAsComboBox();
        var dualColumnViewModel = CreateDualColumnViewModel(applyAsComboBox);
        var labelFieldView = new DualColumnLabelFieldView(dualColumnViewModel);

        AddControlToMainPanel(labelFieldView);
    }

    private ComboBox CreateApplyAsComboBox()
    {
        return new ComboBox
        {
            Items =
            {
                new ComboBoxItem { Content = VelocityOptionText },
                new ComboBoxItem { Content = SensitivityOptionText }
            },
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private DualColumnLabelFieldViewModel CreateDualColumnViewModel(ComboBox applyAsComboBox)
    {
        var viewModel = new DualColumnLabelFieldViewModel();
        viewModel.AddField(ApplyAsLabelText, applyAsComboBox);
        return viewModel;
    }

    private void AddControlToMainPanel(DualColumnLabelFieldView labelFieldView)
    {
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(labelFieldView);
    }
}
