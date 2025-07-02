using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class AccelerationProfileSettingsView : UserControl
{
    private const int NoneAccelerationIndex = 0;
    private const int FormulaAccelerationIndex = 1;
    private const int LUTAccelerationIndex = 2;
    private const int AccelerationFieldInsertIndex = 0;
    private const int FormulaViewInsertIndex = 1;
    private const int LUTViewInsertIndex = 2;
    private const double ViewContainerTopMargin = 8.0;

    private DualColumnLabelFieldView? _accelerationField;
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

        CreateAccelerationComboBox(viewModel);
        CreateAccelerationField();
        CreateViewContainers();
        AddControlsToMainPanel();
        UpdateViewBasedOnSelection();
    }

    private void CreateAccelerationComboBox(AccelerationProfileSettingsViewModel viewModel)
    {
        _accelerationComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = viewModel,
            ItemsSource = viewModel.DefinitionTypesLocal,
            SelectedItem = viewModel.AccelerationBE.DefinitionType.InterfaceValue
        };

        _accelerationComboBox.SelectionChanged += OnAccelerationTypeSelectionChanged;
    }

    private void CreateAccelerationField()
    {
        if (_accelerationComboBox == null)
            return;

        var fieldViewModel = new DualColumnLabelFieldViewModel();
        fieldViewModel.AddField("Acceleration", _accelerationComboBox);
        _accelerationField = new DualColumnLabelFieldView(fieldViewModel);
    }

    private void CreateViewContainers()
    {
        var containerMargin = new Thickness(0, ViewContainerTopMargin, 0, 0);

        _formulaViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = containerMargin,
            IsVisible = false
        };

        _lutViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = containerMargin,
            IsVisible = false
        };
    }

    private void AddControlsToMainPanel()
    {
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        if (mainStackPanel == null || _accelerationField == null ||
            _formulaViewContainer == null || _lutViewContainer == null)
            return;

        mainStackPanel.Children.Insert(AccelerationFieldInsertIndex, _accelerationField);
        mainStackPanel.Children.Insert(FormulaViewInsertIndex, _formulaViewContainer);
        mainStackPanel.Children.Insert(LUTViewInsertIndex, _lutViewContainer);
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
        HideAllViews();

        switch (selectedIndex)
        {
            case NoneAccelerationIndex:
                break;
            case FormulaAccelerationIndex:
                ShowFormulaView(viewModel);
                break;
            case LUTAccelerationIndex:
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
        if (_formulaViewContainer == null)
            return;

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
        if (_lutViewContainer == null)
            return;

        var lutView = new AccelerationLUTSettingsView
        {
            DataContext = viewModel.AccelerationLUTSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _lutViewContainer.Content = lutView;
        _lutViewContainer.IsVisible = true;
    }
}
