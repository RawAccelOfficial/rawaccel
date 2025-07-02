using System.Collections.ObjectModel;
using userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DeviceGroupSelectorViewModel : ViewModelBase
    {
        protected DeviceGroupModel selectedEntry = null!;

        public DeviceGroupSelectorViewModel(DeviceModel device, DeviceGroups deviceGroupsBE)
        {
            Device = device;
            DeviceGroupsBE = deviceGroupsBE;
            RefreshSelectedDeviceGroup();
        }

        protected DeviceModel Device { get; }
        protected DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<DeviceGroupModel> DeviceGroupEntries =>
            DeviceGroupsBE.DeviceGroupModels;

        public DeviceGroupModel SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (DeviceGroupEntries.Contains(value))
                {
                    Device.DeviceGroup = value;
                    selectedEntry = value;
                }
            }
        }

        public bool IsValid { get; set; }

        public void RefreshSelectedDeviceGroup()
        {
            if (!DeviceGroupEntries.Contains(Device.DeviceGroup))
            {
                IsValid = false;
                SelectedEntry = DeviceGroups.DefaultDeviceGroup;
                return;
            }

            IsValid = true;
            selectedEntry = Device.DeviceGroup;
        }
    }
}
