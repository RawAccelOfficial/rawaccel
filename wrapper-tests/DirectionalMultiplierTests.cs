using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace wrapper_tests
{
    [TestClass]
    public class DirectionalMultiplierTests
    {
        const string JsonKey = "Directional multiplier (angle, multiplier pairs; 0 = right, 90 = down)";

        [TestMethod]
        public void Given_DefaultProfile_DoesNotChangeInput()
        {
            var accel = new ManagedAccel(new Profile());

            foreach (var (x, y) in new[] { (3, 0), (0, -5), (-7, 2), (4, 9) })
            {
                var output = accel.Accelerate(x, y, 1, 1);
                Assert.AreEqual(x, output.Item1);
                Assert.AreEqual(y, output.Item2);
            }
        }

        [TestMethod]
        public void Given_Points_InterpolatesByAngle()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 0.5f);

            var cases = new[]
            {
                (x: 10, y: 0, expected: 1.0),     // right
                (x: 0, y: 10, expected: 2.0),     // down
                (x: 10, y: 10, expected: 1.5),    // 45 degrees
                (x: -10, y: 0, expected: 1.0),    // left
                (x: 0, y: -10, expected: 0.5),    // up
                (x: 10, y: -10, expected: 0.75),  // 315 degrees, between 270 and 360 (= 0)
            };

            foreach (var c in cases)
            {
                AssertScaled(c.x, c.y, c.expected, AccelerateFirst(profile, c.x, c.y));
            }
        }

        [TestMethod]
        public void Given_Points_WrapsAround360()
        {
            var profile = MakeProfile(45, 1.5f, 315, 0.5f);

            // 0 degrees is halfway along the 315 -> 405 (= 45) segment
            AssertScaled(10, 0, 1.0, AccelerateFirst(profile, 10, 0));

            double angle = Math.Atan2(-1, 10) * 180 / Math.PI + 360;
            double expected = 0.5 + (angle - 315) / 90;
            AssertScaled(10, -1, expected, AccelerateFirst(profile, 10, -1));

            // 270 degrees is on the regular 45 -> 315 segment
            AssertScaled(0, -10, 1.5 - 225.0 / 270, AccelerateFirst(profile, 0, -10));
        }

        [TestMethod]
        public void Given_SinglePoint_IsConstant()
        {
            var profile = MakeProfile(123, 1.25f);

            foreach (var (x, y) in new[] { (3, 0), (0, -5), (-7, 2), (4, 9) })
            {
                AssertScaled(x, y, 1.25, AccelerateFirst(profile, x, y));
            }
        }

        [TestMethod]
        public void Given_OutputDPIRatios_AppliesAfterThem()
        {
            var profile = MakeProfile(0, 1.25f);
            profile.lrOutputDPIRatio = 1.2;

            AssertScaled(-10, 0, 1.2 * 1.25, AccelerateFirst(profile, -10, 0));
        }

        [TestMethod]
        public void Given_AlternatingSlowInput_UsesRecentDirection()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            var accel = new ManagedAccel(profile);

            // a slow 45 degree movement, reported as alternating single counts
            for (int i = 0; i < 50; i++)
            {
                accel.Accelerate(1, 0, 1, 1);
                accel.Accelerate(0, 1, 1, 1);
            }

            var output = accel.Accelerate(1, 0, 1, 1);

            // without smoothing this would be exactly 1 (0 degrees)
            Assert.AreEqual(1.5, output.Item1, 0.1);
            Assert.AreEqual(0, output.Item2);
        }

        [TestMethod]
        public void Given_Reversal_ResetsDirection()
        {
            var profile = MakeProfile(0, 1, 180, 2);
            var accel = new ManagedAccel(profile);

            for (int i = 0; i < 10; i++)
            {
                accel.Accelerate(10, 0, 1, 1);
            }

            AssertScaled(-10, 0, 2, accel.Accelerate(-10, 0, 1, 1));
        }

        [TestMethod]
        public void Given_Idle_ResetsDirection()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            var accel = new ManagedAccel(profile);

            for (int i = 0; i < 10; i++)
            {
                accel.Accelerate(10, 0, 1, 1);
            }

            var output = accel.Accelerate(0, 10, 1, 100);

            Assert.AreEqual(0, output.Item1);
            Assert.AreEqual(20, output.Item2, 0.01);
        }

        [TestMethod]
        public void CreateStatelessCopy_DoesNotSmoothDirection()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            var accel = new ManagedAccel(profile).CreateStatelessCopy();

            for (int i = 0; i < 10; i++)
            {
                accel.Accelerate(0, 1, 1, 1);
            }

            AssertScaled(1, 0, 1, accel.Accelerate(1, 0, 1, 1));
        }

        [TestMethod]
        public void Validation_AcceptsValidPoints()
        {
            Assert.IsTrue(IsValid(new Profile()));
            Assert.IsTrue(IsValid(MakeProfile(0, 0.5f, 90, 2, 359.9f, 1)));
        }

        [TestMethod]
        public void Validation_RejectsInvalidPoints()
        {
            Assert.IsFalse(IsValid(MakeProfile(0, 1, 90)), "odd number of values");
            Assert.IsFalse(IsValid(MakeProfile(0, 2.5f)), "multiplier above max");
            Assert.IsFalse(IsValid(MakeProfile(0, 0.25f)), "multiplier below min");
            Assert.IsFalse(IsValid(MakeProfile(360, 1)), "angle of 360");
            Assert.IsFalse(IsValid(MakeProfile(-1, 1)), "negative angle");
            Assert.IsFalse(IsValid(MakeProfile(90, 1, 90, 1)), "repeated angle");
            Assert.IsFalse(IsValid(MakeProfile(90, 1, 45, 1)), "decreasing angle");
            Assert.IsFalse(IsValid(MakeProfile(float.NaN, 1)), "NaN angle");

            var tooMany = new Profile();
            tooMany.directionalMultiplierLength = tooMany.directionalMultiplier.Length + 2;
            Assert.IsFalse(IsValid(tooMany), "too many points");
        }

        [TestMethod]
        public void Json_Default_HasEmptyList()
        {
            string json = DriverConfig.GetDefault().ToJSON();
            StringAssert.Contains(json, $"\"{JsonKey}\": []");

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(errors);
            Assert.AreEqual(0, cfg.profiles[0].directionalMultiplierLength);
        }

        [TestMethod]
        public void Json_WithoutKey_LoadsAsDisabled()
        {
            string json = Regex.Replace(DriverConfig.GetDefault().ToJSON(), $"\\s*\"{Regex.Escape(JsonKey)}\": \\[\\],", "");
            Assert.IsFalse(json.Contains("Directional multiplier"));

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(errors);
            Assert.AreEqual(0, cfg.profiles[0].directionalMultiplierLength);
            Assert.AreEqual(256, cfg.profiles[0].directionalMultiplier.Length);
        }

        [TestMethod]
        public void Json_WithPoints_RoundTrips()
        {
            string json = DriverConfig.GetDefault().ToJSON()
                .Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": [0, 1.1, 180, 0.9]");

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(errors);

            var (again, againErrors) = DriverConfig.Convert(cfg.ToJSON()).ToValueTuple();
            Assert.IsNull(againErrors);

            var prof = again.profiles[0];
            Assert.AreEqual(4, prof.directionalMultiplierLength);
            var points = new float[4];
            Array.Copy(prof.directionalMultiplier, points, 4);
            CollectionAssert.AreEqual(new[] { 0, 1.1f, 180, 0.9f }, points);

            AssertScaled(-10, 0, 0.9, again.accels[0].Accelerate(-10, 0, 1, 1));
        }

        [TestMethod]
        public void Json_WithInvalidPoints_ReportsError()
        {
            string json = DriverConfig.GetDefault().ToJSON()
                .Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": [0, 3]");

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(cfg);
            StringAssert.Contains(errors, "directional multiplier must be between 0.5 and 2");
        }

        private static Profile MakeProfile(params float[] points)
        {
            var profile = new Profile();
            Array.Copy(points, profile.directionalMultiplier, points.Length);
            profile.directionalMultiplierLength = points.Length;
            return profile;
        }

        // a new accel has no direction history, so the first input's own direction is used
        private static Tuple<double, double> AccelerateFirst(Profile profile, int x, int y)
        {
            return new ManagedAccel(profile).Accelerate(x, y, 1, 1);
        }

        private static bool IsValid(Profile profile)
        {
            return new ProfileErrors(new List<Profile> { profile }).Empty();
        }

        private static void AssertScaled(int x, int y, double expected, Tuple<double, double> output)
        {
            Assert.AreEqual(x * expected, output.Item1, 1e-5, $"x of ({x}, {y})");
            Assert.AreEqual(y * expected, output.Item2, 1e-5, $"y of ({x}, {y})");
        }
    }
}
