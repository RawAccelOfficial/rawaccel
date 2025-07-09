using System.Diagnostics;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DeviceViewModel : ViewModelBase
    {
        public DeviceViewModel(BE.DeviceModel deviceBE, BE.DevicesModel devicesBE)
        {
            DeviceBE = deviceBE;
            DevicesBE = devicesBE;

            NameField = new NamedEditableFieldViewModel(DeviceBE.Name);

            HWIDField = new NamedEditableFieldViewModel(DeviceBE.HardwareID);

            DPIField = new NamedEditableFieldViewModel(DeviceBE.DPI);

            PollRateField = new NamedEditableFieldViewModel(DeviceBE.PollRate);

            IgnoreBool = new EditableBoolViewModel(DeviceBE.Ignore);
            IgnoreBool.PropertyChanged += OnIgnoreBoolChanged;

            DeviceGroup = new DeviceGroupSelectorViewModel(DeviceBE, DevicesBE.DeviceGroups);

            DeleteCommand = new RelayCommand(
                () => DeleteSelf());
        }

        protected BE.DeviceModel DeviceBE { get; }

        protected BE.DevicesModel DevicesBE { get; }

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

        public void DeleteSelf()
        {
            bool success = DevicesBE.RemoveDevice(DeviceBE);
            Debug.Assert(success);
        }
    }
}
