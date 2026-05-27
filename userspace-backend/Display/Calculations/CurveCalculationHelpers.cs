using System.Collections.Generic;

namespace userspace_backend.Display.Calculations
{
    // TODO: Replace with curvature-adaptive sampling or adjacent.
    public static class CurveCalculationHelpers
    {
        // Non-zero floor: CurvePreview divides output by MouseSpeed, and any log grid
        // needs a positive minimum. Do not lower this to 0.
        public const double SlowestHandSpeed = 0.05;
        public const double FastestHandSpeed = 200;   // max charted hand speed
        public const int CurvePointsResolution = 256;

        public static IReadOnlyList<double> CalculateCurvePointSpeeds()
        {
            int count = CurvePointsResolution;
            List<double> curvePointSpeeds = new List<double>(count);

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / (count - 1);
                curvePointSpeeds.Add(SlowestHandSpeed + t * (FastestHandSpeed - SlowestHandSpeed));
            }

            return curvePointSpeeds;
        }
    }
}
