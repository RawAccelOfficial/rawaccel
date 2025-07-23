using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DevicesListViewModel : ViewModelBase
    {
        public DevicesListViewModel(BE.DevicesModel devicesBE)
        {
            DevicesBE = devicesBE;
            DeviceViews = [];
            UpdateDeviceViews();
            DevicesBE.Devices.CollectionChanged += DevicesCollectionChanged;

            AddDeviceCommand = new RelayCommand(
                () => TryAddDevice());
        }

        protected BE.DevicesModel DevicesBE { get; }

        public ObservableCollection<BE.DeviceModel> Devices => DevicesBE.Devices;

        public ObservableCollection<DeviceViewModel> DeviceViews { get; }

        public ICommand AddDeviceCommand { get; }

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            UpdateDeviceViews();

        public void UpdateDeviceViews()
        {
            DeviceViews.Clear();
            for (int i = 0; i < DevicesBE.Devices.Count; i++)
            {
                var device = DevicesBE.Devices[i];
                bool isDefault = i == 0;
                DeviceViews.Add(new DeviceViewModel(device, DevicesBE, isDefault));
            }
        }

        public bool TryAddDevice() => DevicesBE.TryAddDevice();
    }
}