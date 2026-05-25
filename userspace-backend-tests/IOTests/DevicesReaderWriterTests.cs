using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.IO;

namespace userspace_backend_tests.IOTests
{
    [TestClass]
    public class DevicesReaderWriterTests
    {
        public static string TestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "DevicesReaderWriter");
        public static string ExpectedOutputs = Path.Combine(TestDirectory, "ExpectedOutputs");
        public static string TestInputs = Path.Combine(TestDirectory, "Inputs");

        [TestMethod]
        public void GivenEmptyInput_Reads()
        {
            var reader = new DevicesReaderWriter();
            string readInputPath = Path.Combine(TestInputs, "readEmptyInput.json");
            var actualReadDevices = reader.Read(readInputPath);
            var expectedDevices = new List<Device>();
            AssertSequenceEqual(expectedDevices, actualReadDevices);
        }

        [TestMethod]
        public void GivenInvalidInput_FailsToRead()
        {
            var reader = new DevicesReaderWriter();
            string readInputPath = Path.Combine(TestInputs, "readInvalidInput.json");
            Exception foundException = null;
            try
            {
                var actualReadDevices = reader.Read(readInputPath);
            }
            catch (Exception ex)
            {
                foundException = ex;
            }

            Assert.IsNotNull(foundException);
        }

        [TestMethod]
        public void GivenValidInput_Reads()
        {
            var device1 = new Device()
            {
                Name = "Superlight (wireless)",
                HWID = "blah-blah-blah",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device2 = new Device()
            {
                Name = "Superlight (wired)",
                HWID = "blah-blah-blah-2",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device3 = new Device()
            {
                Name = "Vaxee Outset AX",
                HWID = "this-is-a-fake-hwid",
                DeviceGroup = "Test Mice",
                DPI = 1600,
                PollingRate = 1000,
            };

            var expectedDevices = new List<Device>() { device1, device2, device3 };

            var reader = new DevicesReaderWriter();

            string readInputPath = Path.Combine(TestInputs, "readWellFormedInput.json");
            var actualReadDevices = reader.Read(readInputPath);
            AssertSequenceEqual(expectedDevices, actualReadDevices);
        }

        [TestMethod]
        public void GivenValidInput_Writes()
        {
            var device1 = new Device()
            {
                Name = "Superlight (wireless)",
                HWID = "blah-blah-blah",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device2 = new Device()
            {
                Name = "Superlight (wired)",
                HWID = "blah-blah-blah-2",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device3 = new Device()
            {
                Name = "Vaxee Outset AX",
                HWID = "this-is-a-fake-hwid",
                DeviceGroup = "Test Mice",
                DPI = 1600,
                PollingRate = 1000,
                Ignore = true,
            };

            var devices = new List<Device>() { device1, device2, device3 };

            var writer = new DevicesReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testWriteDevices.json");
            Clean(writePath);
            writer.Write(writePath, devices);

            Assert.IsTrue(File.Exists(writePath));
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedWriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [TestMethod]
        public void GivenValidInput_OverwritesExistingFile()
        {
            var device1 = new Device()
            {
                Name = "Superlight (wireless)",
                HWID = "blah-blah-blah",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device2 = new Device()
            {
                Name = "Superlight (wired)",
                HWID = "blah-blah-blah-2",
                DeviceGroup = "Logitech Mice",
                DPI = 12000,
                PollingRate = 2000,
            };

            var device3 = new Device()
            {
                Name = "Vaxee Outset AX",
                HWID = "this-is-a-fake-hwid",
                DeviceGroup = "Test Mice",
                DPI = 1600,
                PollingRate = 1000,
            };

            var devices = new List<Device>() { device1, device2, device3 };

            var writer = new DevicesReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testOverwriteDevices.json");
            Clean(writePath);
            writer.Write(writePath, devices);
            Assert.IsTrue(File.Exists(writePath));

            // Change device name
            device3 = new Device()
            {
                Name = "The user has changed the name of this device",
                HWID = "this-is-a-fake-hwid",
                DeviceGroup = "Test Mice",
                DPI = 1600,
                PollingRate = 1000,
            };

            // .Net 8 must be doing something new with lists and references because this is necessary but shouldn't be
            devices[2] = device3;

            // Write again with change device to ensure that old settings are overwritten and not appended.
            writer.Write(writePath, devices);

            Assert.IsTrue(File.Exists(writePath));
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedOverwriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        private void Clean(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var expectedEnumerated = expected.ToList();
            var actualEnumerated = actual.ToList();

            Assert.AreEqual(expectedEnumerated.Count, actualEnumerated.Count, "Sequences have different length.");

            for (int i = 0; i < expectedEnumerated.Count; i++)
            {
                Assert.AreEqual(expectedEnumerated[i], actualEnumerated[i]);
            }
        }
    }
}