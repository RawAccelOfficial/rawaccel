using Avalonia.Controls;
using Avalonia.Input;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
    }

    public void OnItemPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ProfileListElementViewModel item)
        {
            if (DataContext is ProfileListViewModel viewModel)
            {
                viewModel.SetSelectedProfile(item);
            }
        }
    }
}