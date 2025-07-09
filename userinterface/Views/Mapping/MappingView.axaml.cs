using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using userinterface.ViewModels.Mapping;
using userspace_backend.Model;

namespace userinterface.Views.Mapping;

public partial class MappingView : UserControl
{
    public MappingView()
    {
        InitializeComponent();
    }

    public void AddMappingSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0
            && DataContext is MappingViewModel viewModel)
        {
            DeviceGroupSelectorToAddMapping.ItemsSource = Enumerable.Empty<DeviceGroupModel>();
            viewModel.HandleAddMappingSelection(e);
            DeviceGroupSelectorToAddMapping.ItemsSource = viewModel.MappingBE.DeviceGroupsStillUnmapped;

            if (!viewModel.MappingBE.DeviceGroupsStillUnmapped.Any())
            {
                AddEntryButton.Flyout?.Hide();
            }
        }
    }
}