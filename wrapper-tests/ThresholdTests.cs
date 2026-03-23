using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace wrapper_tests
{
    [TestClass]
    public class ThresholdTests
    {
        /// <summary>
        /// This test checks for "jumps" in output when crossing the sharpness = 16 threshold.
        /// It compares the Driver's optimized logic (which clamps at 16) with a 
        /// pure mathematical simulator that does NOT clamp.
        /// </summary>
        [TestMethod]
        public void SynchronousAccel_ThresholdCrossing_IsContinuous()
        {
            double syncSpeed = 20;
            double gamma = 0.5;
            double motivity = 1.3;

            // smooth = 0.03125 exactly results in sharpness = 16
            // We test values slightly above and slightly below this threshold
            double[] smoothValues = { 0.03126, 0.03125, 0.03124, 0.0 };

            foreach (double smooth in smoothValues)
            {
                var profile = new Profile();
                profile.outputDPI = 1000;
                profile.argsX.mode = AccelMode.synchronous;
                profile.argsX.gain = false;
                profile.argsX.syncSpeed = syncSpeed;
                profile.argsX.gamma = gamma;
                profile.argsX.motivity = motivity;
                profile.argsX.smooth = smooth;

                var accel = new ManagedAccel(profile);

                // Pure Math Simulator (Ignores the 16 threshold)
                var pureSimulator = new UnclampedSynchronousSimulator(syncSpeed, motivity, gamma, smooth);

                // Test with various input counts (1 to 100)
                for (int input = 1; input < 100; input += 5)
                {
                    Tuple<double, double> output = accel.Accelerate(input, 0, 1, 10);

                    double inputSpeed = (double)input / 10.0;
                    double expectedOutput = (double)input * pureSimulator.Accelerate(inputSpeed);

                    // We expect the difference to be very small (continuity check)
                    // Precision of 0.01% is usually safe for mouse input
                    double delta = Math.Abs(output.Item1 - expectedOutput);
                    double relativeError = delta / expectedOutput;

                    Assert.IsTrue(relativeError < 0.001,
                        $"Discontinuity found at smooth={smooth}, input={input}. " +
                        $"Driver: {output.Item1}, Pure Math: {expectedOutput}, Error: {relativeError:P}");
                }
            }
        }
    }

    /// <summary>
    /// A simulator that follows the raw math WITHOUT the sharpness >= 16 optimization.
    /// This allows us to see what the "true" curve would look like without the clamp.
    /// </summary>
    public class UnclampedSynchronousSimulator
    {
        public UnclampedSynchronousSimulator(double syncSpeed, double motivity, double gamma, double smooth)
        {
            SyncSpeed = syncSpeed;
            Motivity = motivity;
            Gamma = gamma;
            Sharpness = smooth <= 0 ? 100 : 0.5 / smooth; // Use a very high number instead of clamping
        }

        public double SyncSpeed { get; }
        public double Motivity { get; }
        public double Gamma { get; }
        public double Sharpness { get; }

        public double Accelerate(double inputSpeed)
        {
            if (inputSpeed == SyncSpeed) return 1.0;

            double logSyncSpeed = Math.Log(SyncSpeed);
            double logMotivity = Math.Log(Motivity);
            double gammaConst = Gamma / logMotivity;

            double logX = Math.Log(inputSpeed);
            double logDiff = logX - logSyncSpeed;

            double logSpace = Math.Abs(gammaConst * logDiff);
            double exponent = Math.Pow(Math.Tanh(Math.Pow(logSpace, Sharpness)), 1 / Sharpness);

            if (logDiff < 0) exponent = -exponent;

            return Math.Exp(exponent * logMotivity);
        }
    }
}
