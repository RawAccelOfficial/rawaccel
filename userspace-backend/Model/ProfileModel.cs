using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using userspace_backend.Common;
using userspace_backend.Display;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public class ProfileModel : EditableSettingsCollection<DATA.Profile>
    {
        public ProfileModel(DATA.Profile dataObject, IModelValueValidator<string> nameValidator) : base(dataObject)
        {
            NameValidator = nameValidator;
            XCurvePreview = new CurvePreview();
            YCurvePreview = new CurvePreview();
            RecalculateDriverDataAndCurvePreview();
        }

        public string CurrentNameForDisplay => Name.CurrentValidatedValue;

        public EditableSetting<string> Name { get; set; }

        public EditableSetting<int> OutputDPI { get; set; }

        public EditableSetting<double> YXRatio { get; set; }

        public AccelerationModel Acceleration { get; set; }

        public HiddenModel Hidden { get; set; }

        public Profile CurrentValidatedDriverProfile { get; protected set; }

        public ICurvePreview XCurvePreview { get; protected set; }

        public ICurvePreview YCurvePreview { get; protected set; }

        [Obsolete("Use XCurvePreview instead")]
        public ICurvePreview CurvePreview => XCurvePreview;

        protected IModelValueValidator<string> NameValidator { get; }

        public override DATA.Profile MapToData()
        {
            return new DATA.Profile()
            {
                Name = Name.ModelValue,
                OutputDPI = OutputDPI.ModelValue,
                YXRatio = YXRatio.ModelValue,
                Acceleration = Acceleration.MapToData(),
                Hidden = Hidden.MapToData(),
            };
        }

        protected void AnyNonPreviewPropertyChangedEventHandler(object? send, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(EditableSetting<string>.CurrentValidatedValue)))
            {
                RecalculateDriverData();
            }
        }

        protected void AnyCurvePreviewPropertyChangedEventHandler(object? send, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(EditableSetting<string>.CurrentValidatedValue)))
            {
                RecalculateDriverDataAndCurvePreview();
            }
        }

        protected void AnyCurveSettingCollectionChangedEventHandler(object? sender, EventArgs e)
        {
            // All settings collections currently require curve preview to be re-generated
            RecalculateDriverDataAndCurvePreview();
        }

        protected void RecalculateDriverData()
        {
            CurrentValidatedDriverProfile = DriverHelpers.MapProfileModelToDriver(this);
        }

        protected void RecalculateDriverDataAndCurvePreview()
        {
            RecalculateDriverData();

            // Generate X curve points (original behavior)
            XCurvePreview.GeneratePoints(CurrentValidatedDriverProfile);

            // Generate Y curve points by multiplying X curve outputs by YX ratio
            GenerateYCurvePoints();
        }

        private void GenerateYCurvePoints()
        {
            var yPoints = CreateYCurvePointsFromX(XCurvePreview.Points, YXRatio.CurrentValidatedValue);
            YCurvePreview.SetPoints(yPoints);
        }

        private List<CurvePoint> CreateYCurvePointsFromX(ObservableCollection<CurvePoint> xPoints, double yxRatio)
        {
            return xPoints.Select(xPoint => new CurvePoint
            {
                MouseSpeed = xPoint.MouseSpeed,
                Output = xPoint.Output * yxRatio
            }).ToList();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, OutputDPI, YXRatio];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [Acceleration, Hidden];
        }

        protected override void InitEditableSettingsAndCollections(DATA.Profile dataObject)
        {
           Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);

            OutputDPI = new EditableSetting<int>(
                displayName: "Output DPI",
                initialValue: dataObject.OutputDPI,
                parser: UserInputParsers.IntParser,
                validator: ModelValueValidators.DefaultIntValidator,
                localizationKey: "ProfileOutputDPI");

            YXRatio = new EditableSetting<double>(
                displayName: "Y/X Ratio",
                initialValue: dataObject.YXRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator,
                localizationKey: "ProfileYXRatio");

            Acceleration = new AccelerationModel(dataObject.Acceleration);
            Hidden = new HiddenModel(dataObject.Hidden);

            // Name and Output DPI do not need to generate a new curve preview
            Name.PropertyChanged += AnyNonPreviewPropertyChangedEventHandler;

            // The rest of settings should generate a new curve preview
            OutputDPI.PropertyChanged += AnyCurvePreviewPropertyChangedEventHandler;
            YXRatio.PropertyChanged += AnyCurvePreviewPropertyChangedEventHandler;
            Acceleration.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;
            Hidden.AnySettingChanged += AnyCurveSettingCollectionChangedEventHandler;
        }
    }
}
