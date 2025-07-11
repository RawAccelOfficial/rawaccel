using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using System.Linq;
using userinterface.ViewModels.Mapping;
using userspace_backend.Model;

namespace userinterface.Views.Mapping
{
    public partial class MappingView : UserControl
    {
        public MappingView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            UpdateActivationIndicator();

            if (DataContext is MappingViewModel viewModel)
            {
                viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MappingViewModel.IsActiveMapping))
                    {
                        UpdateActivationIndicator();
                    }
                };
            }
        }

        private void UpdateActivationIndicator()
        {
            if (this.FindControl<Ellipse>("ActivationIndicator") is Ellipse indicator &&
                DataContext is MappingViewModel viewModel)
            {
                indicator.Classes.Clear();
                indicator.Classes.Add("ActiveIndicator");
                indicator.Classes.Add(viewModel.IsActiveMapping ? "Active" : "Inactive");
            }
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
}
