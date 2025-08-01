using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public class CoalescionModel : EditableSettingsCollection<Coalescion>
    {
        public CoalescionModel(Coalescion dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> InputSmoothingHalfLife { get; set; }

        public EditableSetting<double> ScaleSmoothingHalfLife { get; set; }

        public override Coalescion MapToData()
        {
            return new Coalescion()
            {
                InputSmoothingHalfLife = InputSmoothingHalfLife.ModelValue,
                ScaleSmoothingHalfLife = ScaleSmoothingHalfLife.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ InputSmoothingHalfLife, ScaleSmoothingHalfLife ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Coalescion dataObject)
        {
            InputSmoothingHalfLife = new EditableSetting<double>(
                displayName: "Input Smoothing Half-Life",
                initialValue: dataObject?.InputSmoothingHalfLife ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "CoalescionInputSmoothingHalfLife");
            ScaleSmoothingHalfLife = new EditableSetting<double>(
                displayName: "Scale Smoothing Half-Life",
                initialValue: dataObject?.ScaleSmoothingHalfLife ?? 0,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "CoalescionScaleSmoothingHalfLife");
        }
    }
}
