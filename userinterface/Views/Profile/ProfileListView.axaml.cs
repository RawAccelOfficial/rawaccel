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
            foreach (var item in viewModel.ProfileItems)
            {
                item.UpdateSelection(false);
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ProfileListElementViewModel selectedItem)
            {
                System.Diagnostics.Debug.WriteLine("New selected item" + selectedItem.CurrentNameForDisplay);
                selectedItem.UpdateSelection(true);
            }
        }
    }
}