using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using AccelMode = RawAccel.Contracts.AccelMode;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface ILookupTableDefinitionModel : IAccelDefinitionModelSpecific<LookupTableAccel>
    {
        IEditableSettingSpecific<LookupTableType> ApplyAs { get; }

        IEditableSettingSpecific<LookupTableData> Data { get; }

    }

    public class LookupTableDefinitionModel : EditableSettingsSelectable<LookupTableAccel, Acceleration>, ILookupTableDefinitionModel
    {
        public const string ApplyAsDIKey = $"{nameof(LookupTableDefinitionModel)}.{nameof(ApplyAs)}";
        public const string DataDIKey = $"{nameof(LookupTableDefinitionModel)}.{nameof(Data)}";

        public LookupTableDefinitionModel(
            [FromKeyedServices(ApplyAsDIKey)]IEditableSettingSpecific<LookupTableType> applyAs,
            [FromKeyedServices(DataDIKey)]IEditableSettingSpecific<LookupTableData> data)
            : base([applyAs, data], [])
        {
            ApplyAs = applyAs;
            Data = data;
        }

        public IEditableSettingSpecific<LookupTableType> ApplyAs { get; set; }

        public IEditableSettingSpecific<LookupTableData> Data { get; set; }

        public AccelArgs MapToDriver()
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

        public override LookupTableAccel MapToData()
        {
            return new LookupTableAccel()
            {
                ApplyAs = this.ApplyAs.ModelValue,
                Data = this.Data.ModelValue.Data,
            };
        }

        protected override bool TryMapEditableSettingsFromData(LookupTableAccel data)
        {
            return ApplyAs.TryUpdateModelDirectly(data.ApplyAs)
                & Data.TryUpdateModelDirectly(new LookupTableData(data.Data));
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(LookupTableAccel data)
        {
            return true;
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
