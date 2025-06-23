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
        public static System.TimeSpan AnimationsTime = new System.TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 200);

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
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 2 },
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    GeometrySize = 0,
                    GeometryStroke = null,
                    GeometryFill = null,
                    AnimationsSpeed = AnimationsTime,
                    Name = "Curve Profile",
                    LineSmoothness = 0.2,
                    // PrimaryValue is Y
                    XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                    YToolTipLabelFormatter = (chartPoint) => $"Output: {chartPoint.Coordinate.PrimaryValue:F2}"
                }
            ];

            XAxes =
            [
                new Axis()
                {
                    Name = "Mouse Speed",
                    NameTextSize = 14,
                    NamePaint = new SolidColorPaint(SKColors.White),
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                    TicksPaint = new SolidColorPaint(SKColors.White) { StrokeThickness = 1 },
                    SubseparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(100)) { StrokeThickness = 0.5f },
                    AnimationsSpeed = AnimationsTime,
                    MinLimit = 0,
                }
            ];

            YAxes =
            [
                new Axis()
                {
                    Name = "Output",
                    NameTextSize = 14,
                    NamePaint = new SolidColorPaint(SKColors.White),
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                    TicksPaint = new SolidColorPaint(SKColors.White) { StrokeThickness = 1 },
                    SubseparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(100)) { StrokeThickness = 0.5f },
                    AnimationsSpeed = AnimationsTime,
                    MinLimit = 0,
                }
            ];

            // Tooltip styling
            TooltipTextPaint = new SolidColorPaint(SKColors.White);
            TooltipBackgroundPaint = new SolidColorPaint(SKColors.Black.WithAlpha(180));
        }

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
                XAxes[0].MinLimit = 0;
                XAxes[0].MaxLimit = 100;
                YAxes[0].MinLimit = 0;
                YAxes[0].MaxLimit = 2;
                return;
            }

            var minX = points.Min(p => p.MouseSpeed);
            var maxX = points.Max(p => p.MouseSpeed);
            var minY = points.Min(p => p.Output);
            var maxY = points.Max(p => p.Output);

            if (Math.Abs(maxY - minY) < 0.001)
            {
                var centerY = (minY + maxY) / 2;
                var centerX = (minX + maxX) / 2;

                YAxes[0].MinLimit = Math.Max(0, centerY - 1);
                YAxes[0].MaxLimit = centerY + 1;

                XAxes[0].MinLimit = Math.Max(0, centerX - 50);
                XAxes[0].MaxLimit = centerX + 50;
            }
            else
            {
                var xRange = maxX - minX;
                var yRange = maxY - minY;

                var padding = 0.1;
                var xPadding = xRange * padding;
                var yPadding = yRange * padding;

                XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
                XAxes[0].MaxLimit = maxX + xPadding;
                YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
                YAxes[0].MaxLimit = maxY + yPadding;
            }
        }

    }
}
