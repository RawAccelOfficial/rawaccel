using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using RaAccelMode = RawAccel.Contracts.AccelMode;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface INaturalAccelerationDefinitionModel : IAccelDefinitionModelSpecific<NaturalAccel>
    {
    }

    public class NaturalAccelerationDefinitionModel
        : FormulaAccelerationDefinitionModel<NaturalAccel>,
        INaturalAccelerationDefinitionModel
    {
        public const string DecayRateDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(DecayRate)}";
        public const string InputOffsetDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(InputOffset)}";
        public const string LimitDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(Limit)}";

        public NaturalAccelerationDefinitionModel(
            [FromKeyedServices(DecayRateDIKey)]IEditableSettingSpecific<double> decayRate,
            [FromKeyedServices(InputOffsetDIKey)]IEditableSettingSpecific<double> inputOffset,
            [FromKeyedServices(LimitDIKey)]IEditableSettingSpecific<double> limit)
            : base([decayRate, inputOffset, limit])
        {
            DecayRate = decayRate;
            InputOffset = inputOffset;
            Limit = limit;
        }

        public IEditableSettingSpecific<double> DecayRate { get; set; }

        public IEditableSettingSpecific<double> InputOffset { get; set; }

        public IEditableSettingSpecific<double> Limit { get; set; }

        public override RaAccelArgs MapToDriver()
        {
            return new RaAccelArgs
            {
                mode = RaAccelMode.natural,
                decayRate = DecayRate.ModelValue,
                inputOffset = InputOffset.ModelValue,
                limit = Limit.ModelValue,
            };
        }

        public override NaturalAccel MapToData()
        {
            return new NaturalAccel()
            {
                DecayRate = DecayRate.ModelValue,
                InputOffset = InputOffset.ModelValue,
                Limit = Limit.ModelValue,
            };
        }

        protected override bool TryMapEditableSettingsFromData(NaturalAccel data)
        {
            return DecayRate.TryUpdateModelDirectly(data.DecayRate)
                & InputOffset.TryUpdateModelDirectly(data.InputOffset)
                & Limit.TryUpdateModelDirectly(data.Limit);
        }
    }
}
