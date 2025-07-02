using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileChartView : UserControl
{
    public ProfileChartView()
    {
        InitializeComponent();
    }

    private void RecreateAxes_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileChartViewModel viewModel)
        {
            viewModel.RecreateAxes();
        }
    }

    private void FitToData_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileChartViewModel viewModel)
        {
            viewModel.FitToData();
        }
    }
}
