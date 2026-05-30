using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.IO;

namespace userspace_backend_tests.IOTests
{
    [TestClass]
    public class MappingReaderWriterTests
    {
        public static string TestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "MappingReaderWriter");
        public static string ExpectedOutputs = Path.Combine(TestDirectory, "ExpectedOutputs");
        public static string TestInputs = Path.Combine(TestDirectory, "Inputs");

        [TestMethod]
        public void GivenValidInput_Writes()
        {
            var mappings = new MappingSet()
            {
                Mappings =
                [
                    new Mapping()
                    {
                        Name = "GeneralFavorite",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "Default" },
                            { "Logitech Mice", "Default" },
                            { "Test Mice", "Test" },
                        },
                    },
                    new Mapping()
                    {
                        Name = "MappingForSpecificGame",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "SpecificGameProfile" },
                        },
                    }
                ]
            };

            var writer = new MappingsReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testMapping.json");
            writer.Write(writePath, mappings);

            Assert.IsTrue(File.Exists(writePath));
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedWriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [TestMethod]
        public void GivenValidInput_OverwritesExistingFile()
        {
            var mappings = new MappingSet()
            {
                Mappings =
                [
                    new Mapping()
                    {
                        Name = "GeneralFavorite",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "Default" },
                            { "Logitech Mice", "Default" },
                            { "Test Mice", "Test" },
                        },
                    },
                    new Mapping()
                    {
                        Name = "MappingForSpecificGame",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "SpecificGameProfile" },
                        },
                    }
                ]
            };

            var writer = new MappingsReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testMapping.json");
            writer.Write(writePath, mappings);
            Assert.IsTrue(File.Exists(writePath));

            // Change mapping:
            mappings.Mappings[0].GroupsToProfiles["Test Mice"] = "User has changed this mapping";

            writer.Write(writePath, mappings);
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedOverwriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [TestMethod]
        public void GivenValidInput_Reads()
        {
            var exptectedMappings = new MappingSet()
            {
                Mappings =
                [
                    new Mapping()
                    {
                        Name = "GeneralFavorite",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "Default" },
                            { "Logitech Mice", "Default" },
                            { "Test Mice", "Test" },
                        },
                    },
                    new Mapping()
                    {
                        Name = "MappingForSpecificGame",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping()
                        {
                            { "Default", "SpecificGameProfile" },
                        },
                    }
                ]
            };

            var reader = new MappingsReaderWriter();

            string readInputPath = Path.Combine(TestInputs, "readWellFormedInput.json");
            var actualReadMapping = reader.Read(readInputPath);
            Assert.AreEqual(exptectedMappings, actualReadMapping);
        }

        [TestMethod]
        public void GivenInvalidInput_FailsToRead()
        {
            var reader = new MappingsReaderWriter();
            string readInputPath = Path.Combine(TestInputs, "readInvalidInput.json");
            Exception foundException = null;
            try
            {
                var actualReadMapping = reader.Read(readInputPath);
            }
            catch (Exception ex)
            {
                foundException = ex;
            }

            Assert.IsNotNull(foundException);
        }

        [TestMethod]
        public void GivenEmptyInput_FailsToRead()
        {
            var reader = new MappingsReaderWriter();
            string readInputPath = Path.Combine(TestInputs, "readEmptyInput.json");
            Exception foundException = null;
            try
            {
                var actualReadMapping = reader.Read(readInputPath);
            }
            catch (Exception ex)
            {
                foundException = ex;
            }

            Assert.IsNotNull(foundException);
        }
    }
}
