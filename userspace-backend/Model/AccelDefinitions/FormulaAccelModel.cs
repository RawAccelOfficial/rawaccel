using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.AccelDefinitions.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public class FormulaAccelModel : AccelDefinitionModel<FormulaAccel>
    {
        public FormulaAccelModel(Acceleration dataObject) : base(dataObject)
        {
            FormulaType.PropertyChanged += FormulaTypeChangedEventHandler;
        }


        public EditableSetting<FormulaAccel.AccelerationFormulaType> FormulaType { get; set; }

        public int FormulaTypeIndex { get => (int)FormulaType.ModelValue; }

        protected Dictionary<FormulaAccel.AccelerationFormulaType, IAccelDefinitionModel> FormulaModels { get; set; }

        public override AccelArgs MapToDriver()
        {
            return FormulaModels[FormulaType.ModelValue].MapToDriver();
        }

        public override Acceleration MapToData()
        {
            return FormulaModels[FormulaType.ModelValue].MapToData();
        }

        public IAccelDefinitionModel GetAccelerationModelOfType(FormulaAccel.AccelerationFormulaType formulaType)
        {
            return FormulaModels[formulaType];
        }

        protected void FormulaTypeChangedEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            // When the formula type changes, contained editable settings collections need to correspond to new type
            if (string.Equals(e.PropertyName, nameof(FormulaType.CurrentValidatedValue)))
            {
                GatherEditableSettingsCollections();
            }
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ FormulaType ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [ FormulaModels[FormulaType.ModelValue] ];
        }

        protected override FormulaAccel GenerateDefaultDataObject()
        {
            return new LinearAccel()
            {
                Acceleration = 0.001,
                Cap = 0,
                Offset = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(FormulaAccel dataObject)
        {
            FormulaType = new EditableSetting<FormulaAccel.AccelerationFormulaType>(
                displayName: "Formula Type",
                initialValue: dataObject.FormulaType,
                parser: UserInputParsers.AccelerationFormulaTypeParser,
                validator: ModelValueValidators.DefaultAccelerationFormulaTypeValidator,
                autoUpdateFromInterface: true,
                localizationKey: "AccelFormulaType");

            FormulaModels = new Dictionary<FormulaAccel.AccelerationFormulaType, IAccelDefinitionModel>();

            foreach (FormulaAccel.AccelerationFormulaType formulaType in Enum.GetValues(typeof(FormulaAccel.AccelerationFormulaType)))
            {
                FormulaModels.Add(formulaType, CreateAccelerationDefinitionModelOfType(formulaType, dataObject));
            }
        }

        protected IAccelDefinitionModel CreateAccelerationDefinitionModelOfType(FormulaAccel.AccelerationFormulaType formulaType, Acceleration dataObject)
        {
            switch (formulaType)
            {
                case FormulaAccel.AccelerationFormulaType.Synchronous:
                    return new SynchronousAccelerationDefinitionModel(dataObject);
                case FormulaAccel.AccelerationFormulaType.Jump:
                    return new JumpAccelerationDefinitionModel(dataObject);
                case FormulaAccel.AccelerationFormulaType.Power:
                    return new PowerAccelerationDefinitionModel(dataObject);
                case FormulaAccel.AccelerationFormulaType.Natural:
                    return new NaturalAccelerationDefinitionModel(dataObject);
                case FormulaAccel.AccelerationFormulaType.Classic:
                    return new ClassicAccelerationDefinitionModel(dataObject);
                case FormulaAccel.AccelerationFormulaType.Linear:
                default:
                    return new LinearAccelerationDefinitionModel(dataObject);
            }
        }
    }
}
