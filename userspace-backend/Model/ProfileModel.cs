﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;

namespace userspace_backend.Model
{
    public class ProfileModel : EditableSettingsCollection<Profile>
    {
        public ProfileModel(Profile dataObject) : base(dataObject)
        {
        }
        public EditableSetting<string> Name { get; set; }

        public EditableSetting<int> OutputDPI { get; set; }

        public EditableSetting<double> YXRatio { get; set; }

        public AccelerationModel Acceleration { get; set; }

        public AnisotropyModel Anisotropy { get; set; }

        public HiddenModel Hidden { get; set; }

        public override Profile MapToData()
        {
            return new Profile()
            {
                Name = Name.ModelValue,
                OutputDPI = OutputDPI.ModelValue,
                YXRatio = YXRatio.ModelValue,
                Acceleration = Acceleration.MapToData(),
                Anisotropy = Anisotropy.MapToData(),
                Hidden = Hidden.MapToData(),
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, OutputDPI, YXRatio];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [Acceleration, Anisotropy, Hidden];
        }

        protected override void InitEditableSettingsAndCollections(Profile dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: ModelValueValidators.DefaultStringValidator);
            OutputDPI = new EditableSetting<int>(
                displayName: "Output DPI",
                initialValue: dataObject.OutputDPI,
                parser: UserInputParsers.IntParser,
                validator: ModelValueValidators.DefaultIntValidator);
            YXRatio = new EditableSetting<double>(
                displayName: "Y/X Ratio",
                initialValue: dataObject.YXRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Acceleration = new AccelerationModel(dataObject.Acceleration);
            Anisotropy = new AnisotropyModel(dataObject.Anisotropy);
            Hidden = new HiddenModel(dataObject.Hidden);
        }
    }
}