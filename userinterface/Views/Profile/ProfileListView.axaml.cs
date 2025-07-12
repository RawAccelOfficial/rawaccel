using Avalonia.Controls;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
    }

    public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ProfileListElementViewModel selectedItem)
            {
                viewModel.SetSelectedProfile(selectedItem);
            }
            else
            {
                // No item selected, clear all selections
                viewModel.SetSelectedProfile((ProfileListElementViewModel?)null);
            }
        }
    }
}