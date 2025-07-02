using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using userinterface.Services;
using userspace_backend.Display;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase
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

        // Axis labeling and text
        private const string XAxisName = "Mouse Speed";
        private const string YAxisName = "Output";
        private const int AxisNameTextSize = 14;
        private const int AxisTextSize = 12;

        public static readonly TimeSpan AnimationsTime = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: AnimationMilliseconds);

        public ProfileChartViewModel(ICurvePreview curvePreview)
        {
            _curvePreview = curvePreview;
            Series =
            [
                new LineSeries<CurvePoint>
                {
                    Values = curvePreview.Points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness },
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    GeometrySize = 0,
                    GeometryStroke = null,
                    GeometryFill = null,
                    AnimationsSpeed = AnimationsTime,
                    Name = "Curve Profile",
                    LineSmoothness = 0.2,
                    // Secondary value is x
                    XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                    YToolTipLabelFormatter = (chartPoint) => $"Output: {chartPoint.Coordinate.PrimaryValue:F2}"
                }
            ];

            XAxes = CreateXAxes();
            YAxes = CreateYAxes();
            TooltipTextPaint = new SolidColorPaint(GetThemeColor(AxisTitleBrush));
            TooltipBackgroundPaint = new SolidColorPaint(GetThemeColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha));

            // Subscribe to theme changes
            ThemeService.ThemeChanged += OnThemeChanged;
        }

        private ICurvePreview _curvePreview { get; }

        public ISeries[] Series { get; set; }

        public Axis[] XAxes { get; set; }

        public Axis[] YAxes { get; set; }

        public SolidColorPaint TooltipTextPaint { get; set; }

        public SolidColorPaint TooltipBackgroundPaint { get; set; }

        public void FitToData()
        {
            var points = _curvePreview.Points.ToList();
            if (!points.Any())
            {
                SetDefaultLimits();
                return;
            }

            var (minX, maxX, minY, maxY) = GetDataBounds(points);
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
            ThemeService.ThemeChanged -= OnThemeChanged;
        }

        private static SKColor GetThemeColor(string resourceKey)
        {
            var app = Application.Current;
            if (app?.Resources == null || !app.Resources.TryGetResource(resourceKey, app.ActualThemeVariant, out var resource))
                return GetFallbackColor(resourceKey);

            return resource switch
            {
                SolidColorBrush brush => new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A),
                ImmutableSolidColorBrush brush => new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A),
                _ => GetFallbackColor(resourceKey)
            };
        }

        private static SKColor GetFallbackColor(string resourceKey)
        {
            return resourceKey switch
            {
                var key when key == AxisTitleBrush => SKColors.White,
                var key when key == AxisLabelsBrush => SKColors.LightGray,
                var key when key == AxisSeparatorsBrush => SKColors.Gray,
                var key when key == TooltipBackgroundBrush => SKColors.Black,
                _ => SKColors.White
            };
        }


        private Axis[] CreateXAxes(double? minLimit = null, double? maxLimit = null) =>
        [
            new Axis()
            {
                Name = XAxisName,
                NameTextSize = AxisNameTextSize,
                NamePaint = new SolidColorPaint(GetThemeColor(AxisTitleBrush)),
                LabelsPaint = new SolidColorPaint(GetThemeColor(AxisLabelsBrush)),
                TextSize = AxisTextSize,
                SeparatorsPaint = new SolidColorPaint(GetThemeColor(AxisSeparatorsBrush)) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(GetThemeColor(AxisTitleBrush)) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(GetThemeColor(AxisSeparatorsBrush).WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                AnimationsSpeed = AnimationsTime,
                MinLimit = minLimit ?? 0,
                MaxLimit = maxLimit
            }
        ];

        private Axis[] CreateYAxes(double? minLimit = null, double? maxLimit = null) =>
        [
            new Axis()
            {
                Name = YAxisName,
                NameTextSize = AxisNameTextSize,
                NamePaint = new SolidColorPaint(GetThemeColor(AxisTitleBrush)),
                LabelsPaint = new SolidColorPaint(GetThemeColor(AxisLabelsBrush)),
                TextSize = AxisTextSize,
                SeparatorsPaint = new SolidColorPaint(GetThemeColor(AxisSeparatorsBrush)) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(GetThemeColor(AxisTitleBrush)) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(GetThemeColor(AxisSeparatorsBrush).WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
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

        private static (double minX, double maxX, double minY, double maxY) GetDataBounds(System.Collections.Generic.List<CurvePoint> points)
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
            TooltipTextPaint.Color = GetThemeColor(AxisTitleBrush );
            TooltipBackgroundPaint.Color = GetThemeColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha);

            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);

            // Notify tooltip property changes
            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));
        }
    }
}
