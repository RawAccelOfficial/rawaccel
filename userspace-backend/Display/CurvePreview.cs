using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using userspace_backend.Display.Calculations;
using userspace_backend.Driver;
using RaProfile = RawAccel.Contracts.RawAccelProfile;

namespace userspace_backend.Display
{
    public interface ICurvePreview
    {
        ObservableCollection<CurvePoint> Points { get; }

        void GeneratePoints(RaProfile profile);

        void SetPoints(IEnumerable<CurvePoint> points);
    }

    public class CurvePreview : ICurvePreview
    {
        private readonly IAccelEvaluator evaluator;

        public CurvePreview(IAccelEvaluator evaluator)
        {
            this.evaluator = evaluator;
            Points = new ObservableCollection<CurvePoint>();
            InitPoints();
        }

        public ObservableCollection<CurvePoint> Points { get; }

        public void GeneratePoints(RaProfile profile)
        {
            using IAccelInstance instance = evaluator.CreateInstance(profile);

            foreach (CurvePoint point in Points)
            {
                var (ox, oy) = instance.Accelerate(point.MouseSpeed, 0, dpiFactor: 1, timeMs: 1);
                var outputSpeed = Math.Sqrt(ox * ox + oy * oy);
                point.Output = point.MouseSpeed > 0 ? outputSpeed / point.MouseSpeed : 0.0;
            }
        }

        public void SetPoints(IEnumerable<CurvePoint> points)
        {
            Points.Clear();
            foreach (var point in points)
            {
                Points.Add(point);
            }
        }

        private void InitPoints()
        {
            IReadOnlyList<double> speeds = CurveCalculationHelpers.CalculateCurvePointSpeeds();
            
            foreach (double speed in speeds)
            {
                Points.Add(new CurvePoint() { MouseSpeed = speed, Output = 0.0 });
            }
        }
    }

    public partial class CurvePoint : ObservableObject
    {
        [ObservableProperty]
        public double mouseSpeed;

        [ObservableProperty]
        public double output;
    }

}
