using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;

namespace userspace_backend.Model.AccelDefinitions
{
    public class LookupTableDefinitionModel : AccelDefinitionModel<LookupTableAccel>
    {
        public LookupTableDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<LookupTableType> ApplyAs { get; set; }

        public EditableSetting<LookupTableData> Data { get; set; }

        public override AccelArgs MapToDriver()
        {
            // data in driver profile must be predefined length for marshalling purposes
            var accelArgsData = new float[AccelArgs.MaxLutPoints*2];
            Data.ModelValue.Data.Select(Convert.ToSingle).ToArray().CopyTo(accelArgsData, 0);

            return new AccelArgs
            {
                mode = AccelMode.lut,
                data = accelArgsData,
                length = Data.ModelValue.Data.Length,
            };
        }

        public override Acceleration MapToData()
        {
            return new LookupTableAccel()
            {
                ApplyAs = this.ApplyAs.ModelValue,
                Data = this.Data.ModelValue.Data,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ApplyAs, Data];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override LookupTableAccel GenerateDefaultDataObject()
        {
            return new LookupTableAccel()
            {
                ApplyAs = LookupTableType.Velocity,
                Data = [],
            };
        }

        protected override void InitSpecificSettingsAndCollections(LookupTableAccel dataObject)
        {
            ApplyAs = new EditableSetting<LookupTableType>(
                displayName: "Apply as",
                initialValue: dataObject.ApplyAs,
                parser: UserInputParsers.LookupTableTypeParser,
                validator: ModelValueValidators.DefaultLookupTableTypeValidator,
                localizationKey: "LookupTableApplyAs");
            Data = new EditableSetting<LookupTableData>(
                displayName: "Data",
                initialValue: new LookupTableData(dataObject.Data),
                parser: UserInputParsers.LookupTableDataParser,
                validator: ModelValueValidators.DefaultLookupTableDataValidator,
                localizationKey: "LookupTableData");
        }
    }

    public class LookupTableData : IComparable
    {
        public LookupTableData(double[]? data = null)
        {
            Data = data ?? [];
        }

        public double[] Data { get; set; }

        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return -1;
            }

            double[]? compareTo = obj as double[];

            if (compareTo == null)
            {
                return -1;
            }

            // We are using CompareTo as a stand-in for equality
            return Data.SequenceEqual(compareTo) ? 0 : -1;
        }
    }
}
