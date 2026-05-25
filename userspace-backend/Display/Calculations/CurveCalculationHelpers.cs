using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Display.Calculations
{
    public static class CurveCalculationHelpers
    {
        public const double SlowestHandSpeed = 0.05;
        public const double FastestHandSpeed = 200;
        public const double CurvePointsResolution = 256;

        public static ICollection<double> CalculateCurvePointSpeeds()
        {
            int count = (int)CurvePointsResolution;
            List<double> curvePointSpeeds = new List<double>(count);

            double step = (FastestHandSpeed - SlowestHandSpeed) / (count - 1);
            for (int i = 0; i < count; i++)
            {
                curvePointSpeeds.Add(SlowestHandSpeed + i * step);
            }

            return curvePointSpeeds;
        }
    }
}
