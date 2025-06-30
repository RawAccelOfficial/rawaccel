using System;
using Avalonia.Controls;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls;

public partial class DualColumnLabelFieldView : UserControl
{
    public DualColumnLabelFieldViewModel? ViewModel => DataContext as DualColumnLabelFieldViewModel;

    public DualColumnLabelFieldView()
    {
        InitializeComponent();
        DataContext = new DualColumnLabelFieldViewModel();
    }

    public DualColumnLabelFieldView(DualColumnLabelFieldViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
