using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingViewModel : ViewModelBase
    {
        private ObservableCollection<MappingListElementViewModel> mappingListElements;

        public MappingViewModel(BE.MappingModel mappingBE, BE.MappingsModel mappingsBE, bool isDefault = false)
        {
            MappingBE = mappingBE;
            MappingsBE = mappingsBE;
            IsDefaultMapping = isDefault;

            mappingListElements = new ObservableCollection<MappingListElementViewModel>();
            UpdateMappingListElements();

            MappingBE.IndividualMappings.CollectionChanged += (sender, e) =>
            {
                UpdateMappingListElements();
            };

            MappingBE.DeviceGroupsStillUnmapped.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(HasDeviceGroupsToAdd));
            };

            DeleteCommand = new RelayCommand(() => DeleteSelf());
        }

        public BE.MappingModel MappingBE { get; }

        protected BE.MappingsModel MappingsBE { get; }

        public bool IsDefaultMapping { get; }

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

        public ObservableCollection<MappingListElementViewModel> MappingListElements => mappingListElements;

        public bool HasDeviceGroupsToAdd => MappingBE.DeviceGroupsStillUnmapped.Any();

        public ICommand DeleteCommand { get; }

        private void UpdateMappingListElements()
        {
            foreach (var element in mappingListElements)
            {
                element.Cleanup();
            }

            mappingListElements.Clear();
            for (int i = 0; i < MappingBE.IndividualMappings.Count; i++)
            {
                var mappingGroup = MappingBE.IndividualMappings[i];
                // Consider the first mapping element as default
                bool isDefaultElement = IsDefaultMapping && i == 0;
                mappingListElements.Add(new MappingListElementViewModel(mappingGroup, MappingBE, isDefaultElement));
            }
        }

        public void HandleAddMappingSelection(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is BE.DeviceGroupModel deviceGroup)
            {
                MappingBE.TryAddMapping(deviceGroup.CurrentValidatedValue, BE.ProfilesModel.DefaultProfile.CurrentNameForDisplay);
            }
        }

        public void DeleteSelf()
        {
            bool success = MappingsBE.RemoveMapping(MappingBE);
            Debug.Assert(success);
        }

        public void Cleanup()
        {
            foreach (var element in mappingListElements)
            {
                element.Cleanup();
            }
        }
    }
}