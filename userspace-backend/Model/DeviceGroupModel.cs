using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceGroupModel : EditableSetting<string>, IComparable
    {
        public DeviceGroupModel(string dataObject, IModelValueValidator<string> validator)
            : base("Device Group", dataObject, UserInputParsers.StringParser, validator, localizationKey: "DeviceGroupName")
        {
        }

        public int CompareTo(object? obj)
        {
            DeviceGroupModel other = obj as DeviceGroupModel;

            if (other == null)
            {
                return int.MaxValue;
            }

            return other.ModelValue.CompareTo(ModelValue);
        }

        public override bool Equals(object? obj)
        {
            return obj is DeviceGroupModel model &&
                   string.Equals(model.ModelValue, this.ModelValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelValue);
        }
    }
}
