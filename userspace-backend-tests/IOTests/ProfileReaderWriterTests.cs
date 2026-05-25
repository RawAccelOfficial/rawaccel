using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.IO;

namespace userspace_backend_tests.IOTests
{
    [TestClass]
    public class ProfileReaderWriterTests
    {
        public static string TestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "ProfileReaderWriter");
        public static string ExpectedOutputs = Path.Combine(TestDirectory, "ExpectedOutputs");
        public static string TestInputs = Path.Combine(TestDirectory, "Inputs");

        [TestMethod]
        public void GivenValidInput_Writes()
        {
            var profile = new DATA.Profile()
            {
                Name = "default",
                OutputDPI = 1200,
                YXRatio = 1.3333,
                Acceleration = new Acceleration()
                {
                    Type = Acceleration.AccelerationDefinitionType.None,
                    Anisotropy = new Anisotropy()
                    {
                        Domain = new Vector2()
                        {
                            X = 1,
                            Y = 4,
                        },
                        Range = new Vector2()
                        {
                            X = 1,
                            Y = 1,
                        },
                        LPNorm = 1,
                        CombineXYComponents = false,
                    },
                },
            };

            var writer = new ProfileReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testProfile.json");
            writer.Write(writePath, profile);

            Assert.IsTrue(File.Exists(writePath));
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedWriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
