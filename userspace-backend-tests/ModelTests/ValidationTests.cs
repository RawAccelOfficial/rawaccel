using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        public void RangeValidator_InclusiveMin_RejectsBelowAcceptsAtAndAbove()
        {
            var v = new RangeValidator<int>(min: 1);

            Assert.IsFalse(v.Validate(0), "0 is below an inclusive min of 1.");
            Assert.IsFalse(v.Validate(-5));
            Assert.IsTrue(v.Validate(1), "1 equals an inclusive min of 1.");
            Assert.IsTrue(v.Validate(1000));
        }

        [TestMethod]
        public void RangeValidator_ExclusiveMin_RejectsBoundary()
        {
            var v = new RangeValidator<double>(min: 0, minInclusive: false);

            Assert.IsFalse(v.Validate(0), "0 is excluded when min is exclusive.");
            Assert.IsFalse(v.Validate(-0.1));
            Assert.IsTrue(v.Validate(0.0001));
            Assert.IsTrue(v.Validate(2));
        }

        [TestMethod]
        public void RangeValidator_InclusiveMinZero_AllowsZero()
        {
            var v = new RangeValidator<double>(min: 0);

            Assert.IsTrue(v.Validate(0));
            Assert.IsFalse(v.Validate(-1));
        }

        [TestMethod]
        public void RangeValidator_MaxBound_IsInclusiveByDefault()
        {
            var v = new RangeValidator<int>(min: 1, max: 10);

            Assert.IsTrue(v.Validate(10));
            Assert.IsFalse(v.Validate(11));
        }

        [TestMethod]
        public void DoubleParser_RejectsNaNAndInfinity()
        {
            var parser = new DoubleParser();

            Assert.IsFalse(parser.TryParse("NaN", out _));
            Assert.IsFalse(parser.TryParse("Infinity", out _));
            Assert.IsFalse(parser.TryParse("-Infinity", out _));

            Assert.IsTrue(parser.TryParse("1.5", out double value));
            Assert.AreEqual(1.5, value);
        }
    }
}
