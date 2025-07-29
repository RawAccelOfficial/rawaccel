using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DeviceViewModel : ViewModelBase
    {
        public DeviceViewModel(BE.DeviceModel deviceBE, BE.DevicesModel devicesBE, bool isDefault = false, Func<DeviceViewModel, Task>? animatedDeleteCallback = null)
        {
            DeviceBE = deviceBE;
            DevicesBE = devicesBE;
            IsDefaultDevice = isDefault;
            AnimatedDeleteCallback = animatedDeleteCallback;
            
            Debug.WriteLine($"[DeviceViewModel] Constructor called for device '{deviceBE.Name}', AnimatedDeleteCallback is {(animatedDeleteCallback != null ? "not null" : "null")}");

            NameField = new NamedEditableFieldViewModel(DeviceBE.Name);

            HWIDField = new NamedEditableFieldViewModel(DeviceBE.HardwareID);

            DPIField = new NamedEditableFieldViewModel(DeviceBE.DPI);

            PollRateField = new NamedEditableFieldViewModel(DeviceBE.PollRate);

            IgnoreBool = new EditableBoolViewModel(DeviceBE.Ignore);
            IgnoreBool.PropertyChanged += OnIgnoreBoolChanged;

            DeviceGroup = new DeviceGroupSelectorViewModel(DeviceBE, DevicesBE.DeviceGroups);

            DeleteCommand = new RelayCommand(
                async () => 
                {
                    Debug.WriteLine($"[DeviceViewModel] Delete button pressed for device '{DeviceBE.Name}'");
                    await DeleteWithAnimation();
                });
        }

        internal BE.DeviceModel DeviceBE { get; }

        internal BE.DevicesModel DevicesBE { get; }

        public bool IsDefaultDevice { get; }

        private Func<DeviceViewModel, Task>? AnimatedDeleteCallback { get; }

        public NamedEditableFieldViewModel NameField { get; set; }

        public NamedEditableFieldViewModel HWIDField { get; set; }

        public NamedEditableFieldViewModel DPIField { get; set; }

        public NamedEditableFieldViewModel PollRateField { get; set; }

        public EditableBoolViewModel IgnoreBool { get; set; }

        public DeviceGroupSelectorViewModel DeviceGroup { get; set; }

        public ICommand DeleteCommand { get; }

        public bool IsExpanderEnabled => !IgnoreBool.Value;

        private void OnIgnoreBoolChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditableBoolViewModel.Value))
            {
                OnPropertyChanged(nameof(IsExpanderEnabled));
            }
        }

        private async Task DeleteWithAnimation()
        {
            Debug.WriteLine($"[DeviceViewModel] DeleteWithAnimation called, AnimatedDeleteCallback is {(AnimatedDeleteCallback != null ? "not null" : "null")}");
            
            if (AnimatedDeleteCallback != null)
            {
                Debug.WriteLine($"[DeviceViewModel] Calling AnimatedDeleteCallback");
                await AnimatedDeleteCallback(this);
                Debug.WriteLine($"[DeviceViewModel] AnimatedDeleteCallback completed");
            }
            else
            {
                Debug.WriteLine($"[DeviceViewModel] No AnimatedDeleteCallback available - delete ignored");
            }
        }

        public void DeleteSelf()
        {
            bool success = DevicesBE.RemoveDevice(DeviceBE);
            Debug.Assert(success);
        }
    }
}