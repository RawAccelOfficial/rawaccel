using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingViewModel : ViewModelBase
    {
        private ObservableCollection<MappingListElementViewModel> mappingListElements;

        public MappingViewModel(BE.MappingModel mappingBE, BE.MappingsModel mappingsBE)
        {
            MappingBE = mappingBE;
            MappingsBE = mappingsBE;

            mappingListElements = new ObservableCollection<MappingListElementViewModel>(
                MappingBE.IndividualMappings.Select(mg => new MappingListElementViewModel(mg, MappingBE))
            );

            MappingBE.IndividualMappings.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (BE.MappingGroup newItem in e.NewItems)
                    {
                        mappingListElements.Add(new MappingListElementViewModel(newItem, MappingBE));
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (BE.MappingGroup oldItem in e.OldItems)
                    {
                        var viewModelToRemove = mappingListElements.FirstOrDefault(vm => vm.MappingGroup == oldItem);
                        if (viewModelToRemove != null)
                        {
                            mappingListElements.Remove(viewModelToRemove);
                        }
                    }
                }
            };

            MappingBE.DeviceGroupsStillUnmapped.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(HasDeviceGroupsToAdd));
            };
        }

        public BE.MappingModel MappingBE { get; }

        protected BE.MappingsModel MappingsBE { get; }

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

        public ObservableCollection<MappingListElementViewModel> MappingListElements => mappingListElements;

        public bool HasDeviceGroupsToAdd => MappingBE.DeviceGroupsStillUnmapped.Any();

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
    }
}
