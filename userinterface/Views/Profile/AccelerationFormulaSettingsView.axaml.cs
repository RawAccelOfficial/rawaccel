using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Controls;
using userinterface.Views.Controls;
using BEData = userspace_backend.Data.Profiles.Accel.FormulaAccel;

namespace userinterface.Views.Profile;

public partial class AccelerationFormulaSettingsView : UserControl
{
    private const BEData.AccelerationFormulaType DefaultFormulaType = BEData.AccelerationFormulaType.Synchronous;
    private const int FirstFieldIndex = 1; // Skip Formula Type field when removing

    private DualColumnLabelFieldView? FormulaField;
    private DualColumnLabelFieldViewModel? FormulaFieldViewModel;
    private ComboBox? FormulaTypeCombo;

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

        LogFormulaTypes(viewModel);
        CreateFormulaTypeComboBox();

        if (FormulaTypeCombo == null)
        {
            return;
        }

        CreateFormulaFieldViewModel();
        var currentFormulaType = GetCurrentFormulaType(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaType, viewModel);
        AddControlToMainPanel();
    }

    private void LogFormulaTypes(AccelerationFormulaSettingsViewModel viewModel)
    {
        if (viewModel.FormulaTypesLocal != null)
        {
            foreach (var formulaTypeName in viewModel.FormulaTypesLocal)
            {
                System.Diagnostics.Debug.WriteLine($"  - Item: '{formulaTypeName}'");
            }
        }
    }

    private void CreateFormulaTypeComboBox()
    {
        FormulaTypeCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = DataContext
        };

        FormulaTypeCombo.Bind(ComboBox.ItemsSourceProperty, new Binding("FormulaTypesLocal"));
        FormulaTypeCombo.Bind(ComboBox.SelectedItemProperty, new Binding("FormulaAccelBE.FormulaType.InterfaceValue"));
        FormulaTypeCombo.SelectionChanged += OnFormulaTypeSelectionChanged;
    }

    private void CreateFormulaFieldViewModel()
    {
        if (FormulaTypeCombo == null)
        {
            return;
        }

        FormulaFieldViewModel = new DualColumnLabelFieldViewModel();
        FormulaFieldViewModel.AddField("Formula Type", FormulaTypeCombo);
        FormulaField = new DualColumnLabelFieldView(FormulaFieldViewModel);
    }

    private void AddControlToMainPanel()
    {
        if (FormulaField == null)
        {
            return;
        }

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(FormulaField);
    }

    private void OnFormulaTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AccelerationFormulaSettingsViewModel viewModel || FormulaFieldViewModel == null)
        {
            return;
        }

        viewModel.FormulaAccelBE.FormulaType.TryUpdateFromInterface();
        RemoveFormulaSpecificFields();
        var currentFormulaType = GetCurrentFormulaType(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaType, viewModel);
    }

    private BEData.AccelerationFormulaType GetCurrentFormulaType(string formulaTypeName)
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
        FormulaFieldViewModel!.AddField("Sync Speed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
        FormulaFieldViewModel.AddField("Motivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
        FormulaFieldViewModel.AddField("Gamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
        FormulaFieldViewModel.AddField("Smoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
    }

    private void AddLinearFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("Acceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
        FormulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.LinearSettings.Offset));
        FormulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.LinearSettings.Cap));
    }

    private void AddClassicFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("Acceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
        FormulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
        FormulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
        FormulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
    }

    private void AddPowerFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("Scale", CreateInputControl(formulaSettings.PowerSettings.Scale));
        FormulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
        FormulaFieldViewModel.AddField("Output Offset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
        FormulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.PowerSettings.Cap));
    }

    private void AddNaturalFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("Decay Rate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
        FormulaFieldViewModel.AddField("Input Offset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
        FormulaFieldViewModel.AddField("Limit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
    }

    private void AddJumpFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        FormulaFieldViewModel!.AddField("Smooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
        FormulaFieldViewModel.AddField("Input", CreateInputControl(formulaSettings.JumpSettings.Input));
        FormulaFieldViewModel.AddField("Output", CreateInputControl(formulaSettings.JumpSettings.Output));
    }

    private Control CreateInputControl(object bindingSource)
    {
        if (bindingSource is not EditableFieldViewModel editableField)
            return new TextBox();

        // Set the EditableFieldViewModel to use OnChange mode for real-time updates
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
