using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
    }

    public void AddProfile(object sender, RoutedEventArgs args)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            _ = viewModel.TryAddProfile();
        }
    }

    public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProfileListViewModel viewModel
            && e.AddedItems.Count > 0
            && e.AddedItems[0] is ProfileListElementViewModel selectedItem)
        {
            viewModel.CurrentSelectedProfile = selectedItem.Profile;
        }
    }
}