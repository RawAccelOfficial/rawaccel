using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.AccelDefinitions.Formula;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Accel.FormulaAccel;
using AccelArgs = RawAccel.Contracts.RawAccelAccelArgs;

namespace userspace_backend.Model.AccelDefinitions
{
    public interface IFormulaAccelModel:
        IAccelDefinitionModelSpecific<FormulaAccel>,
        IEditableSettingsSelector<AccelerationFormulaType, FormulaAccel>
    {
        IEditableSettingSpecific<AccelerationFormulaType> FormulaType { get; }

        IEditableSettingSpecific<bool> Gain { get; }
    }

    public class FormulaAccelModel : 
        EditableSettingsSelectableSelector<AccelerationFormulaType, FormulaAccel, Acceleration>,
        IFormulaAccelModel
    {
        public const string SelectionDIKey = $"{nameof(FormulaAccelModel)}.{nameof(Selection)}";
        public const string GainDIKey = $"{nameof(FormulaAccelModel)}.{nameof(Gain)}";

        public FormulaAccelModel(
            IServiceProvider serviceProvider,
            [FromKeyedServices(SelectionDIKey)]IEditableSettingSpecific<AccelerationFormulaType> formulaType,
            [FromKeyedServices(GainDIKey)]IEditableSettingSpecific<bool> gain)
            : base(serviceProvider, formulaType, [gain], [])
        {
            Gain = gain;
        }

        public IEditableSettingSpecific<AccelerationFormulaType> FormulaType => Selection;

        public IEditableSettingSpecific<bool> Gain { get; set; }

        public AccelArgs MapToDriver() => ((IAccelDefinitionModel)Selected)?.MapToDriver() ?? new AccelArgs();

        protected override bool TryMapEditableSettingsCollectionsFromData(FormulaAccel data)
        {
            return Selected.TryMapFromData(data);
        }

        protected override bool TryMapEditableSettingsFromData(FormulaAccel data)
        {
            return FormulaType.TryUpdateModelDirectly(data.FormulaType)
                 & Gain.TryUpdateModelDirectly(data.Gain);
        }
    }
}
