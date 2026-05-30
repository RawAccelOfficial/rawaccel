using System.Collections.Generic;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using RaAccelArgs = RawAccel.Contracts.RawAccelAccelArgs;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    /// <summary>
    /// Shared base for per-formula definition models (Classic, Jump, Linear,
    /// Natural, Power, Synchronous). Each formula is a flat set of leaf double
    /// settings, so collection mapping is a no-op. Subclasses supply
    /// <see cref="MapToDriver"/>, MapToData, and per-field MapFromData.
    /// </summary>
    public abstract class FormulaAccelerationDefinitionModel<TData>
        : EditableSettingsSelectable<TData, FormulaAccel>
        where TData : FormulaAccel
    {
        protected FormulaAccelerationDefinitionModel(IEnumerable<IEditableSetting> curveParameters)
            : base(curveParameters, [])
        {
        }

        public abstract RaAccelArgs MapToDriver();

        // No formula has nested collections -- params are all leaf settings.
        protected sealed override bool TryMapEditableSettingsCollectionsFromData(TData data) => true;
    }
}
