using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.Model.AccelDefinitions;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class LookupTableDataTests
    {
        // Regression: CompareTo previously did `obj as double[]`, but the value
        // passed in is always a LookupTableData, so the cast was always null and
        // CompareTo always returned -1 ("not equal"), even for identical tables.
        [TestMethod]
        public void CompareTo_EqualData_ReportsEqual()
        {
            var a = new LookupTableData(new double[] { 1, 2, 3, 4 });
            var b = new LookupTableData(new double[] { 1, 2, 3, 4 });

            Assert.AreEqual(0, a.CompareTo(b));
        }

        [TestMethod]
        public void CompareTo_DifferentData_ReportsNotEqual()
        {
            var a = new LookupTableData(new double[] { 1, 2, 3, 4 });
            var c = new LookupTableData(new double[] { 1, 2, 3, 5 });

            Assert.AreNotEqual(0, a.CompareTo(c));
        }

        [TestMethod]
        public void CompareTo_Null_ReportsNotEqual()
        {
            var a = new LookupTableData(new double[] { 1, 2 });

            Assert.AreNotEqual(0, a.CompareTo(null));
        }
    }
}
