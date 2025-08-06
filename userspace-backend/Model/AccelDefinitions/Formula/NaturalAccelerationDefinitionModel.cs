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
    public class NaturalAccelerationDefinitionModel : AccelDefinitionModel<NaturalAccel>
    {
        public NaturalAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }
        public EditableSetting<double> DecayRate { get; set; }

        public EditableSetting<double> InputOffset { get; set; }

        public EditableSetting<double> Limit { get; set; }

        public override AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.natural,
                decayRate = DecayRate.ModelValue,
                inputOffset = InputOffset.ModelValue,
                limit = Limit.ModelValue,
            };
        }

        public override Acceleration MapToData()
        {
            return new NaturalAccel()
            {
                DecayRate = DecayRate.ModelValue,
                InputOffset = InputOffset.ModelValue,
                Limit = Limit.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ DecayRate, InputOffset, Limit ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override NaturalAccel GenerateDefaultDataObject()
        {
            return new NaturalAccel()
            {
                DecayRate = 0.1,
                InputOffset = 0,
                Limit = 1.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(NaturalAccel dataObject)
        {
            DecayRate = new EditableSetting<double>(
                displayName: "Decay Rate",
                initialValue: dataObject.DecayRate,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelNaturalDecayRate");
            InputOffset = new EditableSetting<double>(
                displayName: "Input Offset",
                initialValue: dataObject.InputOffset,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelNaturalInputOffset");
            Limit = new EditableSetting<double>(
                displayName: "Limit",
                initialValue: dataObject.Limit,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelNaturalLimit");
        }
    }
}
