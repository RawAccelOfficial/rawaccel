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

        // Create ViewModel and add the field
        _formulaFieldViewModel = new DualColumnLabelFieldViewModel();
        _formulaFieldViewModel.AddField("Formula Type", _formulaTypeCombo);

        _formulaField = new DualColumnLabelFieldView(_formulaFieldViewModel);

        var currentFormulaIndex = GetCurrentFormulaTypeIndex(viewModel.FormulaAccelBE.FormulaType.InterfaceValue);
        AddFormulaSpecificFields(currentFormulaIndex, viewModel);

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(_formulaField);
    }

    private void OnFormulaTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AccelerationFormulaSettingsViewModel viewModel && _formulaFieldViewModel != null)
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
        if (_formulaFieldViewModel == null) return;

        // Remove all fields except the first one (Formula Type)
        while (_formulaFieldViewModel.Fields.Count > 1)
        {
            _formulaFieldViewModel.RemoveField(_formulaFieldViewModel.Fields.Count - 1);
        }
    }

    private void AddFormulaSpecificFields(int formulaTypeIndex, AccelerationFormulaSettingsViewModel formulaSettings)
    {
        if (_formulaFieldViewModel == null) return;

        switch (formulaTypeIndex)
        {
            case 0: // Synchronous
                _formulaFieldViewModel.AddField("Sync Speed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
                _formulaFieldViewModel.AddField("Motivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
                _formulaFieldViewModel.AddField("Gamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
                _formulaFieldViewModel.AddField("Smoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
                break;
            case 1: // Linear
                _formulaFieldViewModel.AddField("Acceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
                _formulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.LinearSettings.Offset));
                _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.LinearSettings.Cap));
                break;
            case 2: // Classic
                _formulaFieldViewModel.AddField("Acceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
                _formulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
                _formulaFieldViewModel.AddField("Offset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
                _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
                break;
            case 3: // Power
                _formulaFieldViewModel.AddField("Scale", CreateInputControl(formulaSettings.PowerSettings.Scale));
                _formulaFieldViewModel.AddField("Exponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
                _formulaFieldViewModel.AddField("Output Offset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
                _formulaFieldViewModel.AddField("Cap", CreateInputControl(formulaSettings.PowerSettings.Cap));
                break;
            case 4: // Natural
                _formulaFieldViewModel.AddField("Decay Rate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
                _formulaFieldViewModel.AddField("Input Offset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
                _formulaFieldViewModel.AddField("Limit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
                break;
            case 5: // Jump
                _formulaFieldViewModel.AddField("Smooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
                _formulaFieldViewModel.AddField("Input", CreateInputControl(formulaSettings.JumpSettings.Input));
                _formulaFieldViewModel.AddField("Output", CreateInputControl(formulaSettings.JumpSettings.Output));
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
