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
    public class PowerAccelerationDefinitionModel : AccelDefinitionModel<PowerAccel>
    {
        public PowerAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Scale { get; set; }

        public EditableSetting<double> Exponent { get; set; }

        public EditableSetting<double> OutputOffset { get; set; }

        public EditableSetting<double> Cap { get; set; }

        public override AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.power,
                scale = Scale.ModelValue,
                exponentPower = Exponent.ModelValue,
                outputOffset = OutputOffset.ModelValue,
                cap = new Vec2<double> { x = 0, y = Cap.ModelValue },
                capMode = CapMode.output,
            };
        }

        public override Acceleration MapToData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Scale, Exponent, OutputOffset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override PowerAccel GenerateDefaultDataObject()
        {
            return new PowerAccel()
            {
                Scale = 1,
                Exponent = 0.05,
                Cap = 0,
                OutputOffset = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(PowerAccel dataObject)
        {
            Scale = new EditableSetting<double>(
                displayName: "Scale",
                initialValue: dataObject.Scale,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelPowerScale");
            Exponent = new EditableSetting<double>(
                displayName: "Exponent",
                initialValue: dataObject.Exponent,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelPowerExponent");
            OutputOffset = new EditableSetting<double>(
                displayName: "Output Offset",
                initialValue: dataObject.OutputOffset,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelPowerOutputOffset");
            Cap = new EditableSetting<double>(
                displayName: "Cap",
                initialValue: dataObject.Cap,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelPowerCap");
        }
    }
}
