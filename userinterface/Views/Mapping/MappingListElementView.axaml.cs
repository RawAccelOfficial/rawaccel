using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels.Mapping;

namespace userinterface.Views.Mapping;

public partial class MappingListElementView : UserControl
{
    public MappingListElementView()
    {
        InitializeComponent();
    }

    public void DeleteMapping(object sender, RoutedEventArgs args)
    {
        if (DataContext is MappingListElementViewModel viewModel)
        {
            viewModel.DeleteSelf();
        }
    }
}