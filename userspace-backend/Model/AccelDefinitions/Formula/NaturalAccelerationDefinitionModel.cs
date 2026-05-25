using Microsoft.Extensions.DependencyInjection;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using AccelMode = RawAccel.Contracts.AccelMode;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public interface INaturalAccelerationDefinitionModel : IAccelDefinitionModelSpecific<NaturalAccel>
    {
    }

    public class NaturalAccelerationDefinitionModel
        : EditableSettingsSelectable<NaturalAccel, FormulaAccel>,
        INaturalAccelerationDefinitionModel
    {
        public const string DecayRateDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(DecayRate)}";
        public const string InputOffsetDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(InputOffset)}";
        public const string LimitDIKey = $"{nameof(NaturalAccelerationDefinitionModel)}.{nameof(Limit)}";

        public NaturalAccelerationDefinitionModel(
            [FromKeyedServices(DecayRateDIKey)]IEditableSettingSpecific<double> decayRate,
            [FromKeyedServices(InputOffsetDIKey)]IEditableSettingSpecific<double> inputOffset,
            [FromKeyedServices(LimitDIKey)]IEditableSettingSpecific<double> limit)
            : base([decayRate, inputOffset, limit], [])
        {
            DecayRate = decayRate;
            InputOffset = inputOffset;
            Limit = limit;
        }

        public IEditableSettingSpecific<double> DecayRate { get; set; }

        public IEditableSettingSpecific<double> InputOffset { get; set; }

        public IEditableSettingSpecific<double> Limit { get; set; }

        public AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.natural,
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

        protected override bool TryMapEditableSettingsCollectionsFromData(NaturalAccel data)
        {
            return true;
        }
    }
}
