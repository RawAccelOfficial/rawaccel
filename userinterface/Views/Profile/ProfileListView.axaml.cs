using Avalonia.Controls;
using userinterface.ViewModels.Profile;
using System.ComponentModel;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileListViewModel.CurrentPosition))
        {
            var viewModel = (ProfileListViewModel)sender;
            var profileElement = this.FindControl<ProfileListElementView>("ProfileElement");
            profileElement?.AnimateToPosition(viewModel.CurrentPosition);
        }
    }
}