using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;
using RaAccelMode = RawAccel.Contracts.AccelMode;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface INoAccelDefinitionModel : IAccelDefinitionModelSpecific<NoAcceleration>
    {
    }

    public class NoAccelDefinitionModel : EditableSettingsSelectable<NoAcceleration, Acceleration>, INoAccelDefinitionModel
    {
        public NoAccelDefinitionModel()
            : base([], [])
        {
            NoAcceleration = new NoAcceleration();
        }

        public NoAcceleration NoAcceleration { get; protected set; }

        public RaAccelArgs MapToDriver()
        {
            return new RaAccelArgs()
            {
                mode = RaAccelMode.noaccel,
            };
        }

        public override NoAcceleration MapToData()
        {
            return NoAcceleration;
        }

        protected override bool TryMapEditableSettingsFromData(NoAcceleration data)
        {
            return true;
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(NoAcceleration data)
        {
            return true;
        }
    }
}
