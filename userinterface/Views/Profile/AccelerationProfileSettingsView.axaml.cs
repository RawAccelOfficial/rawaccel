using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using userinterface.Services;
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

    private DualColumnLabelFieldView? AccelerationField;
    private ContentControl? FormulaViewContainer;
    private ContentControl? LUTViewContainer;
    private LocalizedComboBox? AccelerationComboBox;
    private AnisotropyProfileSettingsView? AnisotropyView;
    private CoalescionProfileSettingsView? CoalescionView;

    public AccelerationProfileSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (AccelerationField == null)
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
        AddControlsToMainPanel(viewModel);
        UpdateViewBasedOnSelection();
    }

    private void CreateAccelerationComboBox(AccelerationProfileSettingsViewModel viewModel)
    {
        AccelerationComboBox = new LocalizedComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            LocalizationKeys = AccelerationProfileSettingsViewModel.DefinitionTypeKeysLocal,
            EnumValues = AccelerationProfileSettingsViewModel.DefinitionTypesLocal
        };

        // Set up custom binding to sync enum values
        AccelerationComboBox.SelectionChanged += (s, e) =>
        {
            if (AccelerationComboBox.SelectedEnumValue != null)
            {
                viewModel.AccelerationBE.DefinitionType.InterfaceValue = AccelerationComboBox.SelectedEnumValue;
                viewModel.AccelerationBE.DefinitionType.TryUpdateFromInterface();
                UpdateViewBasedOnSelection();
            }
        };

        // Set initial selection based on backend value
        var currentValue = viewModel.AccelerationBE.DefinitionType.InterfaceValue;
        if (!string.IsNullOrEmpty(currentValue))
        {
            var matchingItem = AccelerationComboBox.localizedItems.FirstOrDefault(item => item.EnumValue == currentValue);
            if (matchingItem != null)
            {
                AccelerationComboBox.SelectedItem = matchingItem;
            }
        }

        AccelerationComboBox.RefreshItems();
    }

    private void CreateAccelerationField()
    {
        if (AccelerationComboBox == null)
            return;

        var localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        var fieldViewModel = new DualColumnLabelFieldViewModel(localizationService);
        fieldViewModel.AddField("AccelDefinitionType", AccelerationComboBox);
        AccelerationField = new DualColumnLabelFieldView(fieldViewModel);
    }

    private void CreateViewContainers()
    {
        var containerMargin = new Thickness(0, ViewContainerTopMargin, 0, 0);

        FormulaViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = containerMargin,
            IsVisible = false
        };

        LUTViewContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = containerMargin,
            IsVisible = false
        };
    }

    private void AddControlsToMainPanel(AccelerationProfileSettingsViewModel viewModel)
    {
        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        if (mainStackPanel == null || AccelerationField == null ||
            FormulaViewContainer == null || LUTViewContainer == null)
            return;

        mainStackPanel.Children.Insert(AccelerationFieldInsertIndex, AccelerationField);
        mainStackPanel.Children.Insert(FormulaViewInsertIndex, FormulaViewContainer);
        mainStackPanel.Children.Insert(LUTViewInsertIndex, LUTViewContainer);

        AnisotropyView = new AnisotropyProfileSettingsView
        {
            DataContext = viewModel.AnisotropySettings,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsVisible = false
        };

        CoalescionView = new CoalescionProfileSettingsView
        {
            DataContext = viewModel.CoalescionSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsVisible = false
        };

        mainStackPanel.Children.Add(AnisotropyView);
        mainStackPanel.Children.Add(CoalescionView);
    }


    private void UpdateViewBasedOnSelection()
    {
        if (DataContext is not AccelerationProfileSettingsViewModel viewModel || AccelerationComboBox == null)
            return;

        var selectedValue = AccelerationComboBox.SelectedEnumValue;
        var selectedIndex = AccelerationProfileSettingsViewModel.DefinitionTypesLocal.ToList().IndexOf(selectedValue ?? "");
        var isNotNone = selectedIndex != NoneAccelerationIndex;

        HideAllViews();
        UpdateAdditionalFieldsVisibility(isNotNone);

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

    private void UpdateAdditionalFieldsVisibility(bool isVisible)
    {
        if (AnisotropyView != null)
            AnisotropyView.IsVisible = isVisible;
        if (CoalescionView != null)
            CoalescionView.IsVisible = isVisible;
    }

    private void HideAllViews()
    {
        if (FormulaViewContainer != null)
            FormulaViewContainer.IsVisible = false;
        if (LUTViewContainer != null)
            LUTViewContainer.IsVisible = false;
    }

    private void ShowFormulaView(AccelerationProfileSettingsViewModel viewModel)
    {
        if (FormulaViewContainer == null)
            return;

        var formulaView = new AccelerationFormulaSettingsView
        {
            DataContext = viewModel.AccelerationFormulaSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        FormulaViewContainer.Content = formulaView;
        FormulaViewContainer.IsVisible = true;
    }

    private void ShowLUTView(AccelerationProfileSettingsViewModel viewModel)
    {
        if (LUTViewContainer == null)
            return;

        var lutView = new AccelerationLUTSettingsView
        {
            DataContext = viewModel.AccelerationLUTSettings,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        LUTViewContainer.Content = lutView;
        LUTViewContainer.IsVisible = true;
    }
}