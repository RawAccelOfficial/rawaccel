using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;
using userinterface.Controls;
using BEData = userspace_backend.Data.Profiles.Accel.FormulaAccel;

namespace userinterface.Views.Profile;

public partial class AccelerationFormulaSettingsView : UserControl
{
    private const BEData.AccelerationFormulaType DefaultFormulaType = BEData.AccelerationFormulaType.Synchronous;
    private const int FirstFieldIndex = 1; // Skip Formula Type field when removing

    private DualColumnLabelFieldView? FormulaField;
    private DualColumnLabelFieldViewModel? FormulaFieldViewModel;
    private LocalizedComboBox? FormulaTypeCombo;

    public AccelerationFormulaSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (FormulaField == null)
        {
            SetupControls();
        }
    }

    private void SetupControls()
    {
        if (DataContext is not AccelerationFormulaSettingsViewModel viewModel)
        {
            return;
        }

        CreateFormulaTypeComboBox();

        if (FormulaTypeCombo == null)
        {
            return;
        }

        CreateFormulaFieldViewModel();
        var currentFormulaType = GetCurrentFormulaType(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaType, viewModel);
        AddControlToStackPanel();
    }

    private void CreateFormulaTypeComboBox()
    {
        FormulaTypeCombo = new LocalizedComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            LocalizationKeys = AccelerationFormulaSettingsViewModel.FormulaTypeKeysLocal,
            EnumValues = AccelerationFormulaSettingsViewModel.FormulaTypesLocal
        };

        FormulaTypeCombo.SelectionChanged += (s, e) =>
        {
            if (DataContext is AccelerationFormulaSettingsViewModel viewModel && FormulaTypeCombo.SelectedEnumValue != null)
            {
                viewModel.FormulaAccelBE.FormulaType.InterfaceValue = FormulaTypeCombo.SelectedEnumValue;
                viewModel.FormulaAccelBE.FormulaType.TryUpdateFromInterface();
                OnFormulaTypeSelectionChanged();
            }
        };
        
        FormulaTypeCombo.RefreshItems();
    }

    private void CreateFormulaFieldViewModel()
    {
        if (FormulaTypeCombo == null)
        {
            return;
        }

        FormulaFieldViewModel = new DualColumnLabelFieldViewModel();
        FormulaFieldViewModel.AddField("AccelFormulaType", FormulaTypeCombo);
        FormulaField = new DualColumnLabelFieldView(FormulaFieldViewModel);
    }

    private void AddControlToStackPanel()
    {
        if (FormulaField == null)
        {
            return;
        }

        var AcceStackPanel = this.FindControl<StackPanel>("AccelStackPanel");
        AcceStackPanel?.Children.Add(FormulaField);
    }

    private void OnFormulaTypeSelectionChanged()
    {
        if (DataContext is not AccelerationFormulaSettingsViewModel viewModel || FormulaFieldViewModel == null)
        {
            return;
        }

        RemoveFormulaSpecificFields();
        var currentFormulaType = GetCurrentFormulaType(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaType, viewModel);
    }

    private static BEData.AccelerationFormulaType GetCurrentFormulaType(string formulaTypeName)
    {
        if (Enum.TryParse<BEData.AccelerationFormulaType>(formulaTypeName, out var formulaType))
        {
            return formulaType;
        }
        return DefaultFormulaType;
    }

    private void RemoveFormulaSpecificFields()
    {
        if (FormulaFieldViewModel == null)
            return;

        // Remove all fields except the first one (Formula Type)
        while (FormulaFieldViewModel.Fields.Count > FirstFieldIndex)
        {
            FormulaFieldViewModel.RemoveField(FormulaFieldViewModel.Fields.Count - 1);
        }
    }

    private void AddFormulaSpecificFields(BEData.AccelerationFormulaType formulaType, AccelerationFormulaSettingsViewModel formulaSettings)
    {
        if (FormulaFieldViewModel == null)
            return;

        switch (formulaType)
        {
            case BEData.AccelerationFormulaType.Synchronous:
                AddSynchronousFields(formulaSettings);
                break;

            case BEData.AccelerationFormulaType.Linear:
                AddLinearFields(formulaSettings);
                break;

            case BEData.AccelerationFormulaType.Classic:
                AddClassicFields(formulaSettings);
                break;

            case BEData.AccelerationFormulaType.Power:
                AddPowerFields(formulaSettings);
                break;

            case BEData.AccelerationFormulaType.Natural:
                AddNaturalFields(formulaSettings);
                break;

            case BEData.AccelerationFormulaType.Jump:
                AddJumpFields(formulaSettings);
                break;
        }
    }

    private void AddSynchronousFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelSynchronousSyncSpeed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
        FormulaFieldViewModel.AddField("AccelSynchronousMotivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
        FormulaFieldViewModel.AddField("AccelSynchronousGamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
        FormulaFieldViewModel.AddField("AccelSynchronousSmoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
    }

    private void AddLinearFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelLinearAcceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
        FormulaFieldViewModel.AddField("AccelLinearOffset", CreateInputControl(formulaSettings.LinearSettings.Offset));
        FormulaFieldViewModel.AddField("AccelLinearCap", CreateInputControl(formulaSettings.LinearSettings.Cap));
    }

    private void AddClassicFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelClassicAcceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
        FormulaFieldViewModel.AddField("AccelClassicExponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
        FormulaFieldViewModel.AddField("AccelClassicOffset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
        FormulaFieldViewModel.AddField("AccelClassicCap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
    }

    private void AddPowerFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelPowerScale", CreateInputControl(formulaSettings.PowerSettings.Scale));
        FormulaFieldViewModel.AddField("AccelPowerExponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
        FormulaFieldViewModel.AddField("AccelPowerOutputOffset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
        FormulaFieldViewModel.AddField("AccelPowerCap", CreateInputControl(formulaSettings.PowerSettings.Cap));
    }

    private void AddNaturalFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelNaturalDecayRate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
        FormulaFieldViewModel.AddField("AccelNaturalInputOffset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
        FormulaFieldViewModel.AddField("AccelNaturalLimit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
    }

    private void AddJumpFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("AccelJumpSmooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
        FormulaFieldViewModel.AddField("AccelJumpInput", CreateInputControl(formulaSettings.JumpSettings.Input));
        FormulaFieldViewModel.AddField("AccelJumpOutput", CreateInputControl(formulaSettings.JumpSettings.Output));
    }

    private static Control CreateInputControl(object bindingSource)
    {
        if (bindingSource is not EditableFieldViewModel editableField)
            return new TextBox();

        editableField.UpdateMode = UpdateMode.OnChange;

        var editableFieldView = new EditableFieldView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = editableField
        };

        return editableFieldView;
    }
}