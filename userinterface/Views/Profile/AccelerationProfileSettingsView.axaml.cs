using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class AccelerationProfileSettingsView : UserControl
{
    private DualColumnLabelField? _accelerationField;
    private ContentControl? _formulaViewContainer;
    private ContentControl? _lutViewContainer;
    private ComboBox? _accelerationComboBox;

    public AccelerationProfileSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_accelerationField == null)
        {
            SetupControls();
        }
    }

    private void SetupControls()
    {
        if (DataContext is not AccelerationProfileSettingsViewModel viewModel)
            return;

        // Create the main ComboBox for acceleration type
        _accelerationComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Set the DataContext and bindings explicitly to avoid binding context issues
        _accelerationComboBox.DataContext = viewModel;
        _accelerationComboBox.ItemsSource = viewModel.DefinitionTypesLocal;
        _accelerationComboBox.SelectedItem = viewModel.AccelerationBE.DefinitionType.InterfaceValue;

        _accelerationComboBox.SelectionChanged += OnAccelerationTypeSelectionChanged;

        // Create the DualColumnLabelField with the main acceleration field
        _accelerationField = new DualColumnLabelField(
            ("Acceleration", _accelerationComboBox)
        );

        // Create containers for the different views
        _formulaViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Avalonia.Thickness(0, 8, 0, 0),
            IsVisible = false
        };

        _lutViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Avalonia.Thickness(0, 8, 0, 0),
            IsVisible = false
        };

        // Add all controls to the main StackPanel
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        if (mainStackPanel != null)
        {
            mainStackPanel.Children.Insert(0, _accelerationField);
            mainStackPanel.Children.Insert(1, _formulaViewContainer);
            mainStackPanel.Children.Insert(2, _lutViewContainer);
        }

        // Set initial view based on current selection
        UpdateViewBasedOnSelection();
    }

    private void OnAccelerationTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateViewBasedOnSelection();
    }

    private void UpdateViewBasedOnSelection()
    {
        if (DataContext is not AccelerationProfileSettingsViewModel viewModel || _accelerationComboBox == null)
            return;

        var selectedIndex = _accelerationComboBox.SelectedIndex;

        // Hide all views first
        HideAllViews();

        // Show the appropriate view based on selected acceleration type
        switch (selectedIndex)
        {
            case 0: // None - no additional fields
                break;
            case 1: // Formula
                ShowFormulaView(viewModel);
                break;
            case 2: // LUT
                ShowLUTView(viewModel);
                break;
        }
    }

    private void HideAllViews()
    {
        if (_formulaViewContainer != null)
            _formulaViewContainer.IsVisible = false;
        if (_lutViewContainer != null)
            _lutViewContainer.IsVisible = false;
    }

    private void ShowFormulaView(AccelerationProfileSettingsViewModel viewModel)
    {
        if (_formulaViewContainer == null) return;

        // Create the formula settings view with proper data context
        var formulaView = new AccelerationFormulaSettingsView
        {
            DataContext = viewModel.AccelerationFormulaSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _formulaViewContainer.Content = formulaView;
        _formulaViewContainer.IsVisible = true;
    }

    private void ShowLUTView(AccelerationProfileSettingsViewModel viewModel)
    {
        if (_lutViewContainer == null) return;

        // Create the LUT settings view with proper data context
        var lutView = new AccelerationLUTSettingsView
        {
            DataContext = viewModel.AccelerationLUTSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _lutViewContainer.Content = lutView;
        _lutViewContainer.IsVisible = true;
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
