using grapher.Common;
using grapher.Models.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grapher.Models.Calculations.Data
{
    public class AccelDataXYComponential : IAccelData
    {
        public AccelDataXYComponential(
            EstimatedPoints xPoints,
            EstimatedPoints yPoints,
            AccelCalculator calculator)
        {
            X = new AccelChartData();
            Y = new AccelChartData();
            XPoints = xPoints;
            YPoints = yPoints;
            Calculator = calculator;
        }

        public AccelChartData X { get; }

        public AccelChartData Y { get; }

        private EstimatedPoints XPoints { get; }

        private EstimatedPoints YPoints { get; }

        private AccelCalculator Calculator { get; }

        public void CalculateDots(double x, double y, double timeInMs)
        {
            var outX = Math.Abs(x) / timeInMs;
            var outY = Math.Abs(y) / timeInMs;

            (var inXVelocity, var xSensitivity, var xGain) = X.FindPointValuesFromOut(outX);
            XPoints.Velocity.Set(inXVelocity, outX);
            XPoints.Sensitivity.Set(inXVelocity, xSensitivity);
            XPoints.Gain.Set(inXVelocity, xGain);

            (var inYVelocity, var ySensitivity, var yGain) = Y.FindPointValuesFromOut(outY);
            YPoints.Velocity.Set(inYVelocity, outY);
            YPoints.Sensitivity.Set(inYVelocity, ySensitivity);
            YPoints.Gain.Set(inYVelocity, yGain);

        }

        public void Clear()
        {
            X.Clear();
            Y.Clear();
        }

        public void CreateGraphData(ManagedAccel accel, Profile settings)
        {
            Clear();
            var sensFactor = Helper.GetSensitivityFactor(settings);
            var sensY = sensFactor * settings.yxOutputDPIRatio;
            Calculator.Calculate(X, accel, sensFactor, Calculator.SimulatedInputX, settings);
            Calculator.Calculate(Y, accel, sensY, Calculator.SimulatedInputY, settings);
        }
    }
}
