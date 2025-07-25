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
    public class JumpAccelerationDefinitionModel : AccelDefinitionModel<JumpAccel>
    {
        public JumpAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Smooth { get; set; }

        public EditableSetting<double> Input { get; set; }

        public EditableSetting<double> Output { get; set; }

        public override AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.jump,
                smooth = Smooth.ModelValue,
                cap = new Vec2<double> { x = Input.ModelValue, y = Output.ModelValue },
            };
        }

        public override Acceleration MapToData()
        {
            return new JumpAccel()
            {
                Smooth = Smooth.ModelValue,
                Input = Input.ModelValue,
                Output = Output.ModelValue
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Smooth, Input, Output ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override JumpAccel GenerateDefaultDataObject()
        {
            return new JumpAccel()
            {
                Smooth = 0.5,
                Input = 15,
                Output = 1.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(JumpAccel dataObject)
        {
            Smooth = new EditableSetting<double>(
                displayName: "Smooth",
                initialValue: dataObject.Smooth,
                parser: UserInputParsers.DoubleParser, 
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelJumpSmooth");
            Input = new EditableSetting<double>(
                displayName: "Input",
                initialValue: dataObject.Input,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelJumpInput");
            Output = new EditableSetting<double>(
                displayName: "Output",
                initialValue: dataObject.Output,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelJumpOutput");
        }
    }
}
