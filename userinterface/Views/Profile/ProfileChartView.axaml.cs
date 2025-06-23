using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile
{
    public partial class ProfileChartView : UserControl
    {
        public ProfileChartView()
        {
            InitializeComponent();
        }

        private void FitToData_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileChartViewModel vm)
            {
                vm.FitToData();
            }
        }
    }
}
