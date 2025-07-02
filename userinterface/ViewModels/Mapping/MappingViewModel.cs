using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingViewModel : ViewModelBase
    {
        public MappingViewModel(BE.MappingModel mappingBE, BE.MappingsModel mappingsBE)
        {
            MappingBE = mappingBE;
            MappingsBE = mappingsBE;
        }

        public BE.MappingModel MappingBE { get; }

        protected BE.MappingsModel MappingsBE { get; }

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

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
