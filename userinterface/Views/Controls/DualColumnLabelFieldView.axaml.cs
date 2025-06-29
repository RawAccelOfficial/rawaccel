using Avalonia.Controls;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls;

public partial class DualColumnLabelFieldView : UserControl
{
    public DualColumnLabelFieldViewModel ViewModel => (DualColumnLabelFieldViewModel)DataContext!;

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
