using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Interfaces;
using userinterface.Services;
using userspace_backend.Display;
using userspace_backend.Model.EditableSettings;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase, IAsyncInitializable
    {
        // Animation settings
        private const int AnimationMilliseconds = 200;

        // Data fitting and bounds
        private const double DataPaddingRatio = 0.1;

        private const double ToleranceThreshold = 0.001;

        // Default chart limits when no data or centering
        private const int DefaultAxisRange = 50;

        private const int DefaultYRange = 1;
        private const int DefaultMaxX = 100;
        private const int DefaultMaxY = 2;

        // Line and stroke thickness
        private const int MainStrokeThickness = 2;

        private const int StandardStrokeThickness = 1;
        private const float SubStrokeThickness = 0.5f;

        // Color transparency values
        private const byte SubSeparatorAlpha = 100;

        private const byte TooltipBackgroundAlpha = 180;

        // Theme color resource keys
        private static readonly string AxisTitleBrush = "PrimaryTextBrush";
        private static readonly string AxisLabelsBrush = "SecondaryTextBrush";
        private static readonly string AxisSeparatorsBrush = "BorderBrush";
        private static readonly string TooltipBackgroundBrush = "CardBackgroundBrush";

        // Axis labeling and text - will be set by localization service
        private const int AxisNameTextSize = 14;
        private const int AxisTextSize = 12;

        public static readonly TimeSpan AnimationsTime = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: AnimationMilliseconds);

        private readonly IThemeService themeService;
        private readonly LocalizationService localizationService;
        private BE.ProfileModel currentProfileModel = null!;

        public ProfileChartViewModel(IThemeService themeService, LocalizationService localizationService)
        {
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            RecreateAxesCommand = new RelayCommand(() => RecreateAxes());
            FitToDataCommand = new RelayCommand(() => FitToData());
        }

        public bool IsInitialized { get; private set; }

        public bool IsInitializing { get; private set; }

        private ICurvePreview XCurvePreview { get; set; } = null!;

        private ICurvePreview YCurvePreview { get; set; } = null!;

        private EditableSetting<double> YXRatio { get; set; } = null!;

        public void Initialize(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel)
                return;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;
        }

        public Task InitializeAsync()
        {
            if (IsInitializing || IsInitialized || currentProfileModel == null)
                return Task.CompletedTask;

            IsInitializing = true;

            // Initialize chart components - these must be on UI thread
            CreateSeries();
            XAxes = CreateXAxes();
            YAxes = CreateYAxes();
            TooltipTextPaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush));
            TooltipBackgroundPaint = new SolidColorPaint(themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha));

            // Subscribe to theme and language changes
            this.themeService.ThemeChanged += OnThemeChanged;
            this.localizationService.PropertyChanged += OnLocalizationChanged;

            IsInitializing = false;
            IsInitialized = true;

            // Notify property changes for bindings
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));

            return Task.CompletedTask;
        }

        public Task SwitchToProfileAsync(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel && IsInitialized)
                return Task.CompletedTask;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;

            // Update chart data synchronously for instant response
            CreateSeries();

            return Task.CompletedTask;
        }

        public ISeries[] Series { get; set; }

        public Axis[] XAxes { get; set; }

        public Axis[] YAxes { get; set; }

        public SolidColorPaint TooltipTextPaint { get; set; }

        public SolidColorPaint TooltipBackgroundPaint { get; set; }

        public ICommand RecreateAxesCommand { get; }

        public ICommand FitToDataCommand { get; }

        public void FitToData()
        {
            var allPoints = XCurvePreview.Points.ToList();

            if (Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold)
            {
                allPoints.AddRange(YCurvePreview.Points);
            }

            if (allPoints.Count == 0)
            {
                SetDefaultLimits();
                return;
            }

            var (minX, maxX, minY, maxY) = CalculateDataBounds(allPoints);
            if (Math.Abs(maxY - minY) < ToleranceThreshold)
            {
                SetCenteredLimits(minX, maxX, minY, maxY);
            }
            else
            {
                SetPaddedLimits(minX, maxX, minY, maxY);
            }
        }

        public void RecreateAxes(double? xMinLimit = null, double? xMaxLimit = null, double? yMinLimit = null, double? yMaxLimit = null)
        {
            XAxes = CreateXAxes(xMinLimit, xMaxLimit);
            YAxes = CreateYAxes(yMinLimit, yMaxLimit);

            // Notify property changes
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        public void Dispose()
        {
            themeService.ThemeChanged -= OnThemeChanged;
            localizationService.PropertyChanged -= OnLocalizationChanged;
            YXRatio.PropertyChanged -= OnYXRatioChanged;
        }


        private void CreateSeries()
        {
            var seriesList = new List<ISeries>
            {
                new LineSeries<CurvePoint>
                {
                    Values = XCurvePreview.Points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness },
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    GeometrySize = 0,
                    GeometryStroke = null,
                    GeometryFill = null,
                    AnimationsSpeed = AnimationsTime,
                    Name = "X Curve Profile",
                    LineSmoothness = 0.2,
                    XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                    YToolTipLabelFormatter = (chartPoint) => $"X Output: {chartPoint.Coordinate.PrimaryValue:F2}"
                }
            };

            // Add Y curve only if YX ratio is not 1
            if (Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold)
            {
                seriesList.Add(new LineSeries<CurvePoint>
                {
                    Values = YCurvePreview.Points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = MainStrokeThickness },
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    GeometrySize = 0,
                    GeometryStroke = null,
                    GeometryFill = null,
                    AnimationsSpeed = AnimationsTime,
                    Name = "Y Curve Profile",
                    LineSmoothness = 0.2,
                    XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                    YToolTipLabelFormatter = (chartPoint) => $"Y Output: {chartPoint.Coordinate.PrimaryValue:F2}"
                });
            }

            Series = [.. seriesList];
        }

        private void OnYXRatioChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditableSetting<double>.CurrentValidatedValue))
            {
                CreateSeries();
                OnPropertyChanged(nameof(Series));
            }
        }

        private Axis[] CreateXAxes(double? minLimit = null, double? maxLimit = null) =>
        [
            new Axis()
            {
                Name = localizationService?.GetText("ChartAxisMouseSpeed") ?? "Mouse Speed",
                NameTextSize = AxisNameTextSize,
                NamePaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush)),
                LabelsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisLabelsBrush)),
                TextSize = AxisTextSize,
                SeparatorsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisSeparatorsBrush)) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush)) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisSeparatorsBrush).WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                AnimationsSpeed = AnimationsTime,
                MinLimit = minLimit ?? 0,
                MaxLimit = maxLimit
            }
        ];

        private Axis[] CreateYAxes(double? minLimit = null, double? maxLimit = null) =>
        [
            new Axis()
            {
                Name = localizationService?.GetText("ChartAxisOutput") ?? "Output",
                NameTextSize = AxisNameTextSize,
                NamePaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush)),
                LabelsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisLabelsBrush)),
                TextSize = AxisTextSize,
                SeparatorsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisSeparatorsBrush)) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush)) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(themeService.GetCachedColor(AxisSeparatorsBrush).WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                AnimationsSpeed = AnimationsTime,
                MinLimit = minLimit ?? 0,
                MaxLimit = maxLimit
            }
        ];

        private void SetDefaultLimits()
        {
            XAxes[0].MinLimit = 0;
            XAxes[0].MaxLimit = DefaultMaxX;
            YAxes[0].MinLimit = 0;
            YAxes[0].MaxLimit = DefaultMaxY;
        }

        private static (double minX, double maxX, double minY, double maxY) CalculateDataBounds(System.Collections.Generic.List<CurvePoint> points)
        {
            var minX = points.Min(p => p.MouseSpeed);
            var maxX = points.Max(p => p.MouseSpeed);
            var minY = points.Min(p => p.Output);
            var maxY = points.Max(p => p.Output);
            return (minX, maxX, minY, maxY);
        }

        private void SetCenteredLimits(double minX, double maxX, double minY, double maxY)
        {
            var centerY = (minY + maxY) / 2;
            var centerX = (minX + maxX) / 2;
            YAxes[0].MinLimit = Math.Max(0, centerY - DefaultYRange);
            YAxes[0].MaxLimit = centerY + DefaultYRange;
            XAxes[0].MinLimit = Math.Max(0, centerX - DefaultAxisRange);
            XAxes[0].MaxLimit = centerX + DefaultAxisRange;
        }

        private void SetPaddedLimits(double minX, double maxX, double minY, double maxY)
        {
            var xRange = maxX - minX;
            var yRange = maxY - minY;
            var xPadding = xRange * DataPaddingRatio;
            var yPadding = yRange * DataPaddingRatio;
            XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
            XAxes[0].MaxLimit = maxX + xPadding;
            YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
            YAxes[0].MaxLimit = maxY + yPadding;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            TooltipTextPaint.Color = themeService.GetCachedColor(AxisTitleBrush);
            TooltipBackgroundPaint.Color = themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha);

            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);

            // Notify tooltip property changes
            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Recreate axes with current limits but updated localized names
            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);
        }
    }
}