using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using userinterface.Services;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls;

public partial class DualColumnLabelFieldView : UserControl
{
    public DualColumnLabelFieldView()
    {
        InitializeComponent();
        var localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        DataContext = new DualColumnLabelFieldViewModel(localizationService);
    }

    public DualColumnLabelFieldView(DualColumnLabelFieldViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}