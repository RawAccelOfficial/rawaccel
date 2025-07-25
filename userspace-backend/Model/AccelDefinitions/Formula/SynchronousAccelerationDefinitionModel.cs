using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public class SynchronousAccelerationDefinitionModel : AccelDefinitionModel<SynchronousAccel>
    {
        public SynchronousAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Gamma { get; set; }

        public EditableSetting<double> Motivity { get; set; }

        public EditableSetting<double> SyncSpeed { get; set; }

        public EditableSetting<double> Smoothness { get; set; }

        public override AccelArgs MapToDriver()
        {
            return new AccelArgs
            {
                mode = AccelMode.synchronous,
                syncSpeed = SyncSpeed.ModelValue,
                motivity = Motivity.ModelValue,
                gamma = Gamma.ModelValue,
                smooth = Smoothness.ModelValue,
            };
        }

        public override Acceleration MapToData()
        {
            return new SynchronousAccel()
            {
                Gamma = Gamma.ModelValue,
                Motivity = Motivity.ModelValue,
                SyncSpeed = SyncSpeed.ModelValue,
                Smoothness = Smoothness.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Gamma, Motivity, SyncSpeed, Smoothness];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override SynchronousAccel GenerateDefaultDataObject()
        {
            return new SynchronousAccel()
            {
                Gamma = 1,
                Motivity = 1.4,
                SyncSpeed = 12,
                Smoothness = 0.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(SynchronousAccel dataObject)
        {
            Gamma = new EditableSetting<double>(
                displayName: "Gamma",
                initialValue: dataObject.Gamma,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelSynchronousGamma");
            Motivity = new EditableSetting<double>(
                displayName: "Motivity",
                initialValue: dataObject.Motivity,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelSynchronousMotivity");
            SyncSpeed = new EditableSetting<double>(
                displayName: "Sync Speed",
                initialValue: dataObject.SyncSpeed,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelSynchronousSyncSpeed");
            Smoothness = new EditableSetting<double>(
                displayName: "Smoothness",
                initialValue: dataObject.Smoothness,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "AccelSynchronousSmoothness");
        }
    }
}
