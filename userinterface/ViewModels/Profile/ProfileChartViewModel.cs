using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using userspace_backend.Display;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase
    {
        public static System.TimeSpan AnimationsTime = new System.TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 200);

        public ProfileChartViewModel(ICurvePreview curvePreview)
        {
            Series =
            [
                new LineSeries<CurvePoint>
                {
                    Values = curvePreview.Points,
                    Fill = null,
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    GeometrySize = 0,
                    GeometryStroke = null,
                    GeometryFill = null,
                    AnimationsSpeed = AnimationsTime,
                }
            ];
            YAxes =
            [
                new Axis()
                {
                    MinZoomDelta = 1,
                    MinLimit = 0,
                    AnimationsSpeed = AnimationsTime,
                }
            ];
        }

        public ISeries[] Series { get; set; }

        public Axis[] XAxes { get; set; }

        public Axis[] YAxes { get; set; }
    }
}
