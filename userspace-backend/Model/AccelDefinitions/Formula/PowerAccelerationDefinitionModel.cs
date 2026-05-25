using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using AccelMode = RawAccel.Contracts.AccelMode;
using CapMode = RawAccel.Contracts.CapMode;
using Vec2D = RawAccel.Contracts.Vec2<double>;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface IPowerAccelerationDefinitionModel : IAccelDefinitionModelSpecific<PowerAccel>
    {
    }

    public class PowerAccelerationDefinitionModel
        : EditableSettingsSelectable<PowerAccel, FormulaAccel>,
        IPowerAccelerationDefinitionModel
    {
        public const string ScaleDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Scale)}";
        public const string ExponentDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(Exponent)}";
        public const string OutputOffsetDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(OutputOffset)}";
        public const string CapDIKey = $"{nameof(ClassicAccelerationDefinitionModel)}.{nameof(CapDIKey)}";

        public PowerAccelerationDefinitionModel(
            [FromKeyedServices(ScaleDIKey)]IEditableSettingSpecific<double> scale,
            [FromKeyedServices(ExponentDIKey)]IEditableSettingSpecific<double> exponent,
            [FromKeyedServices(OutputOffsetDIKey)]IEditableSettingSpecific<double> outputOffset,
            [FromKeyedServices(CapDIKey)]IEditableSettingSpecific<double> cap)
            : base([scale, exponent, outputOffset, cap], [])
        {
            Scale = scale;
            Exponent = exponent;
            OutputOffset = outputOffset;
            Cap = cap;
        }

        public IEditableSettingSpecific<double> Scale { get; set; }

        public IEditableSettingSpecific<double> Exponent { get; set; }

        public IEditableSettingSpecific<double> OutputOffset { get; set; }

        public IEditableSettingSpecific<double> Cap { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.power,
                scale = Scale.ModelValue,
                exponentPower = Exponent.ModelValue,
                outputOffset = OutputOffset.ModelValue,
                cap = new Vec2D { x = 0, y = Cap.ModelValue },
                capMode = CapMode.output,
            };
        }

        public override PowerAccel MapToData()
        {
            return new PowerAccel()
            {
                Scale = Scale.ModelValue,
                Exponent = Exponent.ModelValue,
                OutputOffset = OutputOffset.ModelValue,
                Cap = Cap.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsFromData(PowerAccel data)
        {
            return Scale.TryUpdateModelDirectly(data.Scale)
                & Exponent.TryUpdateModelDirectly(data.Exponent)
                & OutputOffset.TryUpdateModelDirectly(data.OutputOffset)
                & Cap.TryUpdateModelDirectly(data.Cap);
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(PowerAccel data)
        {
            return true;
        }
    }
}
