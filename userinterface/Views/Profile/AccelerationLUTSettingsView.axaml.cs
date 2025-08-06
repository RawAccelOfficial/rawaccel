using Avalonia.Controls;
using Avalonia.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;
using userinterface.Services;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class AccelerationLUTSettingsView : UserControl
{
    private const string VelocityOptionText = "Velocity";
    private const string SensitivityOptionText = "Sensitivity";

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
        var localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        var viewModel = new DualColumnLabelFieldViewModel(localizationService);
        viewModel.AddField("LookupTableApplyAs", applyAsComboBox);
        return viewModel;
    }

    private void AddControlToStackPanel(DualColumnLabelFieldView labelFieldView)
    {
        var LUTStackPanel = this.FindControl<StackPanel>("LUTStackPanel");
        LUTStackPanel?.Children.Add(labelFieldView);
    }
}