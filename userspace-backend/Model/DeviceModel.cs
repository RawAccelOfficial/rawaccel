using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceModel : EditableSettingsCollection<Device>
    {
        public DeviceModel(
            Device device,
            DeviceGroupModel deviceGroup,
            DeviceModelNameValidator deviceModelNameValidator,
            DeviceModelHWIDValidator deviceModelHWIDValidator)
            : base(device)
        {
            DeviceGroup = deviceGroup;
            Name.Validator = deviceModelNameValidator;
            HardwareID.Validator = deviceModelHWIDValidator;
        }

        public EditableSetting<string> Name { get; protected set; }

        public EditableSetting<string> HardwareID { get; protected set; }

        public EditableSetting<int> DPI { get; protected set; }

        public EditableSetting<int> PollRate { get; protected set; }

        public EditableSetting<bool> Ignore { get; protected set; }

        public DeviceGroupModel DeviceGroup { get; set; }

        protected DeviceModelNameValidator DeviceModelNameValidator { get; }

        protected DeviceModelHWIDValidator DeviceModelHWIDValidator { get; }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, HardwareID, DPI, PollRate, Ignore, DeviceGroup];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(Device device)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: device.Name,
                parser: UserInputParsers.StringParser,
                validator: ModelValueValidators.DefaultStringValidator,
                localizationKey: "DeviceName");
            HardwareID = new EditableSetting<string>(
                displayName: "Hardware ID",
                initialValue: device.HWID,
                parser: UserInputParsers.StringParser,
                validator: ModelValueValidators.DefaultStringValidator,
                localizationKey: "DeviceHardwareID");
            DPI = new EditableSetting<int>(
               displayName: "DPI",
               initialValue: device.DPI,
               parser: UserInputParsers.IntParser,
               validator: ModelValueValidators.DefaultIntValidator,
               localizationKey: "DeviceDPI");
            PollRate = new EditableSetting<int>(
                displayName: "Polling Rate",
                initialValue: device.PollingRate,
                parser: UserInputParsers.IntParser,
                validator: ModelValueValidators.DefaultIntValidator,
                localizationKey: "DevicePollingRate");
            Ignore = new EditableSetting<bool>(
                displayName: "Ignore",
                initialValue: device.Ignore,
                parser: UserInputParsers.BoolParser,
                validator: ModelValueValidators.DefaultBoolValidator,
                localizationKey: "DeviceIgnore");
        }

        public override Device MapToData()
        {
            return new Device()
            {
                Name = this.Name.ModelValue,
                HWID = this.HardwareID.ModelValue,
                DPI = this.DPI.ModelValue,
                PollingRate = this.PollRate.ModelValue,
                Ignore = this.Ignore.ModelValue,
                DeviceGroup = DeviceGroup.ModelValue,
            };
        }
    }
}
