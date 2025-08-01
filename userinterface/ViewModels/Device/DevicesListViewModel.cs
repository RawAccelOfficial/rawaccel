using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Views.Device;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DevicesListViewModel : ViewModelBase
    {
        private DevicesListView? devicesListView;

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

        public void SetView(DevicesListView view)
        {
            devicesListView = view;
            
            // Refresh existing DeviceViewModels to include the animation callback
            UpdateDeviceViews();
        }

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (BE.DeviceModel device in e.NewItems)
                        {
                            int index = DevicesBE.Devices.IndexOf(device);
                            bool isDefault = index == 0;
                            var animateCallback = devicesListView != null ? (Func<DeviceViewModel, Task>)devicesListView.AnimateDeviceDelete : null;
                            var deviceViewModel = new DeviceViewModel(device, DevicesBE, isDefault, animateCallback);
                            DeviceViews.Insert(index, deviceViewModel);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null && e.OldStartingIndex >= 0)
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            DeviceViews.RemoveAt(e.OldStartingIndex);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                default:
                    UpdateDeviceViews();
                    break;
            }
        }

        public void UpdateDeviceViews()
        {
            DeviceViews.Clear();
            for (int i = 0; i < DevicesBE.Devices.Count; i++)
            {
                var device = DevicesBE.Devices[i];
                bool isDefault = i == 0;
                var animateCallback = devicesListView != null ? (Func<DeviceViewModel, Task>)devicesListView.AnimateDeviceDelete : null;
                DeviceViews.Add(new DeviceViewModel(device, DevicesBE, isDefault, animateCallback));
            }
        }

        public bool TryAddDevice() => DevicesBE.TryAddDevice();
    }
}