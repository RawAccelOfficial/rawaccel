﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceGroupModel : EditableSettingsCollection<string>, IComparable
    {
        public DeviceGroupModel(string dataObject) : base(dataObject)
        {
        }

        public EditableSetting<string> Name { get; private set; }

        public int CompareTo(object? obj)
        {
            DeviceGroupModel other = obj as DeviceGroupModel;

            if (other == null)
            {
                return int.MaxValue;
            }

            return other.Name.EditableValue.CompareTo(Name.EditableValue);
        }

        public override bool Equals(object? obj)
        {
            return obj is DeviceGroupModel model &&
                   string.Equals(model.Name.EditableValue, this.Name.EditableValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.EditableValue);
        }

        public override string MapToData()
        {
            return Name.EditableValue;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(string dataObject)
        {
            Name = new EditableSetting<string>(dataObject, UserInputParsers.StringParser);
        }
    }
}
