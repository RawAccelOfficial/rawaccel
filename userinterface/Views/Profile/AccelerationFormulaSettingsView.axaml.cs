using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;
using BEData = userspace_backend.Data.Profiles.Accel.FormulaAccel;

namespace userinterface.Views.Profile;

public partial class AccelerationFormulaSettingsView : UserControl
{
    private DualColumnLabelField? _formulaField;
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

        if (viewModel.FormulaTypesLocal != null)
        {
            foreach (var item in viewModel.FormulaTypesLocal)
            {
                System.Diagnostics.Debug.WriteLine($"  - Item: '{item}'");
            }
        }

        _formulaTypeCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = this.DataContext
        };

        _formulaTypeCombo.Bind(ComboBox.ItemsSourceProperty, new Binding("FormulaTypesLocal"));
        _formulaTypeCombo.Bind(ComboBox.SelectedItemProperty, new Binding("FormulaAccelBE.FormulaType.InterfaceValue"));

        _formulaTypeCombo.SelectionChanged += OnFormulaTypeSelectionChanged;

        _formulaField = new DualColumnLabelField(
            ("Formula Type", _formulaTypeCombo)
        );

        var currentFormulaIndex = GetCurrentFormulaTypeIndex(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaIndex, viewModel);

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(_formulaField);
    }


    private void OnFormulaTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AccelerationFormulaSettingsViewModel viewModel && _formulaField != null)
        {
            viewModel.FormulaAccelBE.FormulaType.TryUpdateFromInterface();

            RemoveFormulaSpecificFields();

            var currentFormulaIndex = GetCurrentFormulaTypeIndex(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
            AddFormulaSpecificFields(currentFormulaIndex, viewModel);
        }
    }

    private int GetCurrentFormulaTypeIndex(string formulaTypeName)
    {
        if (Enum.TryParse<BEData.AccelerationFormulaType>(formulaTypeName, out var formulaType))
        {
            return (int)formulaType;
        }
        return 0;
    }

    private void RemoveFormulaSpecificFields()
    {
        if (_formulaField == null) return;

        // Remove all fields except the first one (Formula Type)
        while (_formulaField.Fields.Count > 1)
        {
            _formulaField.RemoveField(_formulaField.Fields.Count - 1);
        }
    }

    private void AddFormulaSpecificFields(int formulaTypeIndex, AccelerationFormulaSettingsViewModel formulaSettings)
    {
        if (_formulaField == null) return;

        switch (formulaTypeIndex)
        {
            case 0: // Synchronous
                _formulaField.AddField("Sync Speed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
                _formulaField.AddField("Motivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
                _formulaField.AddField("Gamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
                _formulaField.AddField("Smoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
                break;
            case 1: // Linear
                _formulaField.AddField("Acceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
                _formulaField.AddField("Offset", CreateInputControl(formulaSettings.LinearSettings.Offset));
                _formulaField.AddField("Cap", CreateInputControl(formulaSettings.LinearSettings.Cap));
                break;
            case 2: // Classic
                _formulaField.AddField("Acceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
                _formulaField.AddField("Exponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
                _formulaField.AddField("Offset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
                _formulaField.AddField("Cap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
                break;
            case 3: // Power
                _formulaField.AddField("Scale", CreateInputControl(formulaSettings.PowerSettings.Scale));
                _formulaField.AddField("Exponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
                _formulaField.AddField("Output Offset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
                _formulaField.AddField("Cap", CreateInputControl(formulaSettings.PowerSettings.Cap));
                break;
            case 4: // Natural
                _formulaField.AddField("Decay Rate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
                _formulaField.AddField("Input Offset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
                _formulaField.AddField("Limit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
                break;
            case 5: // Jump
                _formulaField.AddField("Smooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
                _formulaField.AddField("Input", CreateInputControl(formulaSettings.JumpSettings.Input));
                _formulaField.AddField("Output", CreateInputControl(formulaSettings.JumpSettings.Output));
                break;
        }
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
