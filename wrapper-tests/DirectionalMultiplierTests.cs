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

        [TestMethod]
        public void Given_RandomCurves_MatchesReferenceInterpolation()
        {
            var random = new Random(351);

            foreach (int count in new[] { 1, 2, 3, 17, 128 })
            {
                for (int trial = 0; trial < 5; trial++)
                {
                    var points = RandomCurve(random, count);
                    var accel = new ManagedAccel(MakeProfile(points)).CreateStatelessCopy();

                    for (int x = -20; x <= 20; x++)
                    {
                        for (int y = -20; y <= 20; y++)
                        {
                            if (x == 0 && y == 0) continue;

                            double angle = Math.Atan2(y, x) * 180 / Math.PI;
                            if (angle < 0) angle += 360;
                            double expected = ReferenceMultiplier(points, angle);

                            AssertScaled(x, y, expected, accel.Accelerate(x, y, 1, 1));
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Given_PointAtEveryAngle_ReturnsPointValues()
        {
            var points = new float[256];
            for (int i = 0; i < 128; i++)
            {
                points[2 * i] = i * 360f / 128;
                points[2 * i + 1] = 0.5f + (i % 7) * 0.25f;
            }

            var accel = new ManagedAccel(MakeProfile(points)).CreateStatelessCopy();

            // 0, 90, 180, 270 degrees land exactly on points 0, 32, 64, 96
            var cases = new[] { (x: 10, y: 0, i: 0), (x: 0, y: 10, i: 32), (x: -10, y: 0, i: 64), (x: 0, y: -10, i: 96) };

            foreach (var c in cases)
            {
                AssertScaled(c.x, c.y, points[2 * c.i + 1], accel.Accelerate(c.x, c.y, 1, 1));
            }
        }

        [TestMethod]
        public void Given_AngleJustBelow360_WrapsToFirstPoint()
        {
            var profile = MakeProfile(0, 1, 270, 0.5f);

            double angle = Math.Atan2(-1, 1000) * 180 / Math.PI + 360;
            AssertScaled(1000, -1, 0.5 + 0.5 * (angle - 270) / 90, AccelerateFirst(profile, 1000, -1));
        }

        [TestMethod]
        public void Given_ZeroInput_ReturnsZero()
        {
            var accel = new ManagedAccel(MakeProfile(0, 1, 90, 2));

            var output = accel.Accelerate(0, 0, 1, 1);
            Assert.AreEqual(0, output.Item1);
            Assert.AreEqual(0, output.Item2);

            // and doesn't poison later input
            AssertScaled(0, 10, 2, accel.Accelerate(0, 10, 1, 1));
        }

        [TestMethod]
        public void Given_Rotation_UsesRotatedDirection()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            profile.rotation = 90;

            // right becomes down after rotation
            var output = AccelerateFirst(profile, 10, 0);
            Assert.AreEqual(0, output.Item1, 1e-9);
            Assert.AreEqual(20, output.Item2, 1e-9);
        }

        [TestMethod]
        public void Given_Snap_UsesSnappedDirection()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            profile.snap = 10;

            // about 5.7 degrees, snapped to 0
            var output = AccelerateFirst(profile, 10, 1);
            Assert.AreEqual(Math.Sqrt(101), output.Item1, 1e-9);
            Assert.AreEqual(0, output.Item2);
        }

        [TestMethod]
        public void Given_ByComponentMode_StillScalesWholeVector()
        {
            var profile = MakeProfile(0, 1, 90, 2, 180, 1, 270, 1);
            profile.inputSpeedArgs.combineMagnitudes = false;

            AssertScaled(10, 10, 1.5, AccelerateFirst(profile, 10, 10));
        }

        [TestMethod]
        public void Given_RandomPackets_NeverChangesDirectionOrLeavesCurveRange()
        {
            var random = new Random(352);
            var points = RandomCurve(random, 12);
            var accel = new ManagedAccel(MakeProfile(points));

            double min = double.MaxValue, max = double.MinValue;
            for (int i = 1; i < points.Length; i += 2)
            {
                min = Math.Min(min, points[i]);
                max = Math.Max(max, points[i]);
            }

            for (int i = 0; i < 5000; i++)
            {
                int x = random.Next(-30, 31);
                int y = random.Next(-30, 31);
                double time = random.Next(1, 4) == 1 ? random.NextDouble() * 100 : 1;
                if (x == 0 && y == 0) continue;

                var output = accel.Accelerate(x, y, 1, time);
                double mx = output.Item1 / (x != 0 ? x : 1);
                double my = output.Item2 / (y != 0 ? y : 1);
                double m = x != 0 ? mx : my;

                if (x != 0 && y != 0) Assert.AreEqual(mx, my, 1e-9, "direction changed");
                if (x == 0) Assert.AreEqual(0, output.Item1);
                if (y == 0) Assert.AreEqual(0, output.Item2);
                Assert.IsTrue(m >= min - 1e-6 && m <= max + 1e-6, $"multiplier {m} outside [{min}, {max}]");
            }
        }

        [TestMethod]
        public void Given_UnvalidatedLength_IsIgnored()
        {
            // the driver doesn't validate what it's given, so bad lengths must not be read
            foreach (int length in new[] { 1000, 258, -4, 1, 0 })
            {
                var profile = MakeProfile(0, 2);
                profile.directionalMultiplierLength = length;

                AssertScaled(10, 5, 1, AccelerateFirst(profile, 10, 5));
            }
        }

        [TestMethod]
        public void Validation_Boundaries()
        {
            var full = new float[256];
            for (int i = 0; i < 128; i++)
            {
                full[2 * i] = i * 2.8f;
                full[2 * i + 1] = 1;
            }
            Assert.IsTrue(IsValid(MakeProfile(full)), "128 points");
            Assert.IsTrue(IsValid(MakeProfile(0, 0.5f, 359.99f, 2)), "range limits");

            Assert.IsFalse(IsValid(MakeProfile(0, float.PositiveInfinity)), "infinite multiplier");
            Assert.IsFalse(IsValid(MakeProfile(0, float.NaN)), "NaN multiplier");
            Assert.IsFalse(IsValid(MakeProfile(float.PositiveInfinity, 1)), "infinite angle");
            Assert.IsFalse(IsValid(MakeProfile(0, 0.4999f)), "just below min");
            Assert.IsFalse(IsValid(MakeProfile(0, 2.0001f)), "just above max");

            var negative = new Profile();
            negative.directionalMultiplierLength = -2;
            Assert.IsFalse(IsValid(negative), "negative length");
        }

        [TestMethod]
        public void Json_NullValue_LoadsAsDisabled()
        {
            string json = DriverConfig.GetDefault().ToJSON()
                .Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": null");

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(errors);
            Assert.AreEqual(0, cfg.profiles[0].directionalMultiplierLength);
            Assert.AreEqual(256, cfg.profiles[0].directionalMultiplier.Length);
        }

        [TestMethod]
        public void Json_OddCountOrTooMany_ReportsError()
        {
            string json = DriverConfig.GetDefault().ToJSON();

            var (_, oddErrors) = DriverConfig.Convert(
                json.Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": [0, 1, 90]")).ToValueTuple();
            StringAssert.Contains(oddErrors, "directional multiplier requires (angle, multiplier) pairs");

            var values = new List<string>();
            for (int i = 0; i < 129; i++)
            {
                values.Add((i * 2.7).ToString(System.Globalization.CultureInfo.InvariantCulture));
                values.Add("1");
            }

            var (_, manyErrors) = DriverConfig.Convert(
                json.Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": [{string.Join(", ", values)}]")).ToValueTuple();
            StringAssert.Contains(manyErrors, "too many directional multiplier points (max=128)");
        }

        [TestMethod]
        public void Json_MultipleProfilesWithLut_RoundTrips()
        {
            string profile = Regex.Match(DriverConfig.GetDefault().ToJSON(), "\"profiles\": \\[\\s*(\\{.*\\})\\s*\\]", RegexOptions.Singleline).Groups[1].Value;

            string first = profile
                .Replace("\"name\": \"default\"", "\"name\": \"a\"")
                .Replace($"\"{JsonKey}\": []", $"\"{JsonKey}\": [0, 1.5, 180, 0.75]");
            string second = profile
                .Replace("\"name\": \"default\"", "\"name\": \"b\"")
                .Replace("\"mode\": \"noaccel\"", "\"mode\": \"lut\"")
                .Replace("\"data\": []", "\"data\": [1, 1, 10, 10, 20, 25]");

            string json = Regex.Replace(DriverConfig.GetDefault().ToJSON(), "\"profiles\": \\[.*?\\n  \\]", $"\"profiles\": [{first}, {second}]", RegexOptions.Singleline);

            var (cfg, errors) = DriverConfig.Convert(json).ToValueTuple();
            Assert.IsNull(errors);

            var (again, againErrors) = DriverConfig.Convert(cfg.ToJSON()).ToValueTuple();
            Assert.IsNull(againErrors);
            Assert.AreEqual(2, again.profiles.Count);

            Assert.AreEqual(4, again.profiles[0].directionalMultiplierLength);
            Assert.AreEqual(0, again.profiles[1].directionalMultiplierLength);
            Assert.AreEqual(AccelMode.lut, again.profiles[1].argsX.mode);
            Assert.AreEqual(6, again.profiles[1].argsX.length);

            AssertScaled(-10, 0, 0.75, again.accels[0].Accelerate(-10, 0, 1, 1));
        }

        private static float[] RandomCurve(Random random, int count)
        {
            var angles = new SortedSet<float>();
            while (angles.Count < count)
            {
                float angle = (float)(random.NextDouble() * 360);
                if (angle < 360) angles.Add(angle);
            }

            var points = new List<float>();
            foreach (float angle in angles)
            {
                points.Add(angle);
                points.Add((float)(0.5 + random.NextDouble() * 1.5));
            }

            return points.ToArray();
        }

        // brute force: find the segment containing the angle, including the one wrapping through 360
        private static double ReferenceMultiplier(float[] points, double angle)
        {
            int n = points.Length / 2;

            for (int i = 0; i < n; i++)
            {
                double ax = points[2 * i];
                double ay = points[2 * i + 1];
                double bx = i + 1 < n ? points[2 * i + 2] : points[0] + 360.0;
                double by = i + 1 < n ? points[2 * i + 3] : points[1];

                foreach (double d in new[] { angle, angle + 360 })
                {
                    if (d >= ax && d < bx)
                    {
                        return ay + (by - ay) * (d - ax) / (bx - ax);
                    }
                }
            }

            throw new InvalidOperationException($"no segment for {angle}");
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
