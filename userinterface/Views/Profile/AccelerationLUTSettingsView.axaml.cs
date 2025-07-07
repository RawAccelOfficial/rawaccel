using Avalonia.Controls;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;

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

        AddControlToStackPanel(labelFieldView);
    }

    private static ComboBox CreateApplyAsComboBox()
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

    private static DualColumnLabelFieldViewModel CreateDualColumnViewModel(ComboBox applyAsComboBox)
    {
        var viewModel = new DualColumnLabelFieldViewModel();
        viewModel.AddField(ApplyAsLabelText, applyAsComboBox);
        return viewModel;
    }

    private void AddControlToStackPanel(DualColumnLabelFieldView labelFieldView)
    {
        var LUTStackPanel = this.FindControl<StackPanel>("LUTStackPanel");
        LUTStackPanel?.Children.Add(labelFieldView);
    }
}