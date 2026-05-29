using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface IAccelDefinitionModel : IEditableSettingsCollectionV2
    {
        RaAccelArgs MapToDriver();
    }

    public interface IAccelDefinitionModelSpecific<T> : IAccelDefinitionModel, IEditableSettingsCollectionSpecific<T> where T : Acceleration
    {
    }    
}
