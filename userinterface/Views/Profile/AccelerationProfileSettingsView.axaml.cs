using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.Views.Profile;

namespace userinterface.Views.Profile;

public partial class AccelerationProfileSettingsView : UserControl
{
    public AccelerationProfileSettingsView()
    {
        InitializeComponent();
    }

    private void OnAccelerationTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AccelerationProfileSettingsViewModel viewModel)
            return;

        var comboBox = sender as ComboBox;
        var selectedIndex = comboBox?.SelectedIndex ?? -1;

        // Clear existing additional fields and hide alternative view
        AccelerationField.ClearAdditionalFields();
        AccelerationField.ShowAdditionalFieldsContent();

        // Add fields based on selected acceleration type
        switch (selectedIndex)
        {
            case 0: // None - no additional fields
                break;
            case 1: // Formula
                AddFormulaFields(viewModel);
                break;
            case 2: // LUT
                ShowLUTView(viewModel);
                break;
        }
    }

    private void AddFormulaFields(AccelerationProfileSettingsViewModel viewModel)
    {
        // Add Formula Type selection with proper alignment
        var formulaTypeCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = viewModel.AccelerationFormulaSettings.FormulaTypesLocal,
            SelectedItem = viewModel.AccelerationFormulaSettings.FormulaAccelBE.FormulaType.InterfaceValue
        };
        formulaTypeCombo.SelectionChanged += (s, e) => OnFormulaTypeSelectionChanged(s, e, viewModel);

        AccelerationField.AddField("Formula Type", formulaTypeCombo);

        // Add initial formula fields based on current selection
        var currentFormulaIndex = formulaTypeCombo.SelectedIndex;
        AddFormulaSpecificFields(currentFormulaIndex, viewModel.AccelerationFormulaSettings);
    }

    private void OnFormulaTypeSelectionChanged(object? sender, SelectionChangedEventArgs e, AccelerationProfileSettingsViewModel parentViewModel)
    {
        var comboBox = sender as ComboBox;
        var selectedIndex = comboBox?.SelectedIndex ?? -1;

        // Remove existing formula-specific fields (keep Formula Type field)
        RemoveFormulaSpecificFields();

        // Add new formula-specific fields
        AddFormulaSpecificFields(selectedIndex, parentViewModel.AccelerationFormulaSettings);
    }

    private void RemoveFormulaSpecificFields()
    {
        // Remove all fields except the first one (Formula Type)
        var fieldsToRemove = AccelerationField.AdditionalFields.Count - 1;
        for (int i = 0; i < fieldsToRemove; i++)
        {
            if (AccelerationField.AdditionalFields.Count > 1)
                AccelerationField.RemoveField(AccelerationField.AdditionalFields.Count - 1);
        }
    }

    private void AddFormulaSpecificFields(int formulaTypeIndex, AccelerationFormulaSettingsViewModel formulaSettings)
    {
        switch (formulaTypeIndex)
        {
            case 0: // Synchronous
                AccelerationField.AddField("Sync Speed", CreateInputControl(formulaSettings.SynchronousSettings.SyncSpeed));
                AccelerationField.AddField("Motivity", CreateInputControl(formulaSettings.SynchronousSettings.Motivity));
                AccelerationField.AddField("Gamma", CreateInputControl(formulaSettings.SynchronousSettings.Gamma));
                AccelerationField.AddField("Smoothness", CreateInputControl(formulaSettings.SynchronousSettings.Smoothness));
                break;
            case 1: // Linear
                AccelerationField.AddField("Acceleration", CreateInputControl(formulaSettings.LinearSettings.Acceleration));
                AccelerationField.AddField("Offset", CreateInputControl(formulaSettings.LinearSettings.Offset));
                AccelerationField.AddField("Cap", CreateInputControl(formulaSettings.LinearSettings.Cap));
                break;
            case 2: // Classic
                AccelerationField.AddField("Acceleration", CreateInputControl(formulaSettings.ClassicSettings.Acceleration));
                AccelerationField.AddField("Exponent", CreateInputControl(formulaSettings.ClassicSettings.Exponent));
                AccelerationField.AddField("Offset", CreateInputControl(formulaSettings.ClassicSettings.Offset));
                AccelerationField.AddField("Cap", CreateInputControl(formulaSettings.ClassicSettings.Cap));
                break;
            case 3: // Power
                AccelerationField.AddField("Scale", CreateInputControl(formulaSettings.PowerSettings.Scale));
                AccelerationField.AddField("Exponent", CreateInputControl(formulaSettings.PowerSettings.Exponent));
                AccelerationField.AddField("Output Offset", CreateInputControl(formulaSettings.PowerSettings.OutputOffset));
                AccelerationField.AddField("Cap", CreateInputControl(formulaSettings.PowerSettings.Cap));
                break;
            case 4: // Natural
                AccelerationField.AddField("Decay Rate", CreateInputControl(formulaSettings.NaturalSettings.DecayRate));
                AccelerationField.AddField("Input Offset", CreateInputControl(formulaSettings.NaturalSettings.InputOffset));
                AccelerationField.AddField("Limit", CreateInputControl(formulaSettings.NaturalSettings.Limit));
                break;
            case 5: // Jump
                AccelerationField.AddField("Smooth", CreateInputControl(formulaSettings.JumpSettings.Smooth));
                AccelerationField.AddField("Input", CreateInputControl(formulaSettings.JumpSettings.Input));
                AccelerationField.AddField("Output", CreateInputControl(formulaSettings.JumpSettings.Output));
                break;
        }
    }

    private void ShowLUTView(AccelerationProfileSettingsViewModel viewModel)
    {
        // Create the LUT settings view with proper data context
        var lutView = new AccelerationLUTSettingsView
        {
            DataContext = viewModel.AccelerationLUTSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Avalonia.Thickness(0, 8, 0, 0) // Add some top margin for spacing
        };

        // Show the LUT view as alternative content
        AccelerationField.ShowAlternativeViewContent(lutView);
    }

    private Control CreateInputControl(object bindingSource)
    {
        // Create a ContentControl with proper alignment to ensure the content fills the available space
        return new ContentControl
        {
            Content = bindingSource,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
