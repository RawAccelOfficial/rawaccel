using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Linq;
using userspace_backend.Display;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase
    {
        private const int AnimationMilliseconds = 200;
        private const double DataPaddingRatio = 0.1;
        private const double ToleranceThreshold = 0.001;
        private const int DefaultAxisRange = 50;
        private const int DefaultYRange = 1;
        private const int DefaultMaxX = 100;
        private const int DefaultMaxY = 2;
        private const int MainStrokeThickness = 2;
        private const int StandardStrokeThickness = 1;
        private const float SubStrokeThickness = 0.5f;
        private const byte SubSeparatorAlpha = 100;
        private const byte TooltipBackgroundAlpha = 180;

        public static readonly TimeSpan AnimationsTime = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: AnimationMilliseconds);

        private readonly ICurvePreview _curvePreview;

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
            TooltipTextPaint = new SolidColorPaint(SKColors.White);
            TooltipBackgroundPaint = new SolidColorPaint(SKColors.Black.WithAlpha(TooltipBackgroundAlpha));
        }

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public SolidColorPaint TooltipTextPaint { get; set; }
        public SolidColorPaint TooltipBackgroundPaint { get; set; }

        private Axis[] CreateXAxes() =>
        [
            new Axis()
            {
                Name = "Mouse Speed",
                NameTextSize = 14,
                NamePaint = new SolidColorPaint(SKColors.White),
                LabelsPaint = new SolidColorPaint(SKColors.White),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(SKColors.White) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                AnimationsSpeed = AnimationsTime,
                MinLimit = 0,
            }
        ];

        private Axis[] CreateYAxes() =>
        [
            new Axis()
            {
                Name = "Output",
                NameTextSize = 14,
                NamePaint = new SolidColorPaint(SKColors.White),
                LabelsPaint = new SolidColorPaint(SKColors.White),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = StandardStrokeThickness },
                TicksPaint = new SolidColorPaint(SKColors.White) { StrokeThickness = StandardStrokeThickness },
                SubseparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                AnimationsSpeed = AnimationsTime,
                MinLimit = 0,
            }
        ];

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
    }
}
