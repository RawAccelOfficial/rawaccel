using System.Collections.Generic;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    /// <summary>
    /// Shared base for the per-formula definition models (Classic, Jump, Linear,
    /// Natural, Power, Synchronous). Every formula is a flat set of leaf
    /// double settings with no nested collections, so the collection mapping is a
    /// no-op for all of them. Subclasses supply the formula-specific
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

        public abstract AccelArgs MapToDriver();

        // No formula has nested settings collections; its parameters are all leaf settings.
        protected sealed override bool TryMapEditableSettingsCollectionsFromData(TData data) => true;
    }
}
