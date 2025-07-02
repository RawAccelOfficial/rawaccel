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

    private DualColumnLabelFieldView? _formulaField;
    private DualColumnLabelFieldViewModel? _formulaFieldViewModel;
    private ComboBox? _formulaTypeCombo;

    public AccelerationFormulaSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_formulaField == null)
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
        if (_formulaTypeCombo == null)
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
        _formulaTypeCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = DataContext
        };
        _formulaTypeCombo.Bind(ComboBox.ItemsSourceProperty, new Binding("FormulaTypesLocal"));
        _formulaTypeCombo.Bind(ComboBox.SelectedItemProperty, new Binding("FormulaAccelBE.FormulaType.InterfaceValue"));
        _formulaTypeCombo.SelectionChanged += OnFormulaTypeSelectionChanged;
    }

    private void CreateFormulaFieldViewModel()
    {
        if (_formulaTypeCombo == null)
        {
            return;
        }

        _formulaFieldViewModel = new DualColumnLabelFieldViewModel();
        _formulaFieldViewModel.AddField("Formula Type", _formulaTypeCombo);
        _formulaField = new DualColumnLabelFieldView(_formulaFieldViewModel);
    }

    private void AddControlToMainPanel()
    {
        if (_formulaField == null)
        {
            return;
        }

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(_formulaField);
    }

    private void OnFormulaTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AccelerationFormulaSettingsViewModel viewModel || _formulaFieldViewModel == null)
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
        if (_formulaFieldViewModel == null)
            return;

        // Remove all fields except the first one (Formula Type)
        while (_formulaFieldViewModel.Fields.Count > FirstFieldIndex)
        {
            _formulaFieldViewModel.RemoveField(_formulaFieldViewModel.Fields.Count - 1);
        }
    }

    private void AddFormulaSpecificFields(BEData.AccelerationFormulaType formulaType, AccelerationFormulaSettingsViewModel formulaSettings)
    {
        if (_formulaFieldViewModel == null)
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
        _formulaFieldViewModel!.AddField("Sync Speed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
        _formulaFieldViewModel.AddField("Motivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
        _formulaFieldViewModel.AddField("Gamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
        _formulaFieldViewModel.AddField("Smoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
    }

    private void AddLinearFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        _formulaFieldViewModel!.AddField("Acceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
        _formulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.LinearSettings.Offset));
        _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.LinearSettings.Cap));
    }

    private void AddClassicFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        _formulaFieldViewModel!.AddField("Acceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
        _formulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
        _formulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
        _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
    }

    private void AddPowerFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        _formulaFieldViewModel!.AddField("Scale", CreateInputControl(formulaSettings.PowerSettings.Scale));
        _formulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
        _formulaFieldViewModel.AddField("Output Offset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
        _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.PowerSettings.Cap));
    }

    private void AddNaturalFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        _formulaFieldViewModel!.AddField("Decay Rate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
        _formulaFieldViewModel.AddField("Input Offset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
        _formulaFieldViewModel.AddField("Limit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
    }

    private void AddJumpFields(AccelerationFormulaSettingsViewModel formulaSettings)
    {
        _formulaFieldViewModel!.AddField("Smooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
        _formulaFieldViewModel.AddField("Input", CreateInputControl(formulaSettings.JumpSettings.Input));
        _formulaFieldViewModel.AddField("Output", CreateInputControl(formulaSettings.JumpSettings.Output));
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
