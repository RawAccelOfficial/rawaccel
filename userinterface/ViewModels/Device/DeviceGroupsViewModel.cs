using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DeviceGroupsViewModel : ViewModelBase
    {
        public DeviceGroupsViewModel(BE.DeviceGroups deviceGroupsBE)
        {
            DeviceGroupsBE = deviceGroupsBE;
            DeviceGroupViews = [];
            UpdateDeviceGroupViews();
            DeviceGroupsBE.DeviceGroupModels.CollectionChanged += DeviceGroupsCollectionChanged;

            AddDeviceGroupCommand = new RelayCommand(
                () => TryAddNewDeviceGroup());
        }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<BE.DeviceGroupModel> DeviceGroups => DeviceGroupsBE.DeviceGroupModels;

        public ObservableCollection<DeviceGroupViewModel> DeviceGroupViews { get; }

        public ICommand AddDeviceGroupCommand { get; }

        private void DeviceGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            UpdateDeviceGroupViews();

        public void UpdateDeviceGroupViews()
        {
            DeviceGroupViews.Clear();
            foreach (BE.DeviceGroupModel deviceGroup in DeviceGroupsBE.DeviceGroupModels)
            {
                DeviceGroupViews.Add(new DeviceGroupViewModel(deviceGroup, DeviceGroupsBE));
            }
        }

        public bool TryAddNewDeviceGroup() => DeviceGroupsBE.TryAddDeviceGroup();
    }
}
