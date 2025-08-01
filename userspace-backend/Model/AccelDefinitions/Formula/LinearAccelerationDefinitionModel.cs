using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public class LinearAccelerationDefinitionModel : AccelDefinitionModel<LinearAccel>
    {
        public LinearAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Acceleration { get; set; }

        public EditableSetting<double> Offset { get; set;  }

        public EditableSetting<double> Cap { get; set; }

        public override AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.classic,
                acceleration = Acceleration.ModelValue,
                exponentClassic = 2,
                inputOffset = Offset.ModelValue,
                cap = new Vec2<double> { x = 0, y = Cap.ModelValue },
                capMode = CapMode.output,
            };
        }

        public override Acceleration MapToData()
        {
            return new LinearAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Acceleration, Offset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override LinearAccel GenerateDefaultDataObject()
        {
            return new LinearAccel()
            {
                Acceleration = 0.001,
                Offset = 0,
                Cap = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(LinearAccel dataObject)
        {
            Acceleration = new EditableSetting<double>(
                displayName: "Acceleration",
                initialValue: dataObject.Acceleration,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelLinearAcceleration");
            Offset = new EditableSetting<double>(
                displayName: "Offset",
                initialValue: dataObject.Offset,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelLinearOffset");
            Cap = new EditableSetting<double>(
                displayName: "Cap",
                initialValue: dataObject.Cap,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelLinearCap");
        }
    }
}
