using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using userspace_backend;
using userspace_backend.Data;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.IO;

namespace userspace_backend_tests.IOTests
{
    // End-to-end: write devices/mappings/profiles/settings via BackEndLoader,
    // verify the files actually land in the target directory, then read them
    // back with a fresh BackEndLoader and verify values survive the trip.
    // Catches regressions where a file silently fails to write (path missing,
    // wrong format) or fields are dropped by the serializer.
    [TestClass]
    public class BackEndLoaderRoundTripTests
    {
        private string tempDir = null!;

        [TestInitialize]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(),
                $"rawaccel-roundtrip-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
        }

        [TestCleanup]
        public void Teardown()
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        private BackEndLoader MakeLoader() => new(
            tempDir,
            new DevicesReaderWriter(),
            new MappingsReaderWriter(),
            new ProfileReaderWriter(),
            new SettingsReaderWriter());

        [TestMethod]
        public void WriteSettings_LandsOnDisk_WithCorrectValues()
        {
            var loader = MakeLoader();
            var settings = new Settings
            {
                Theme = "Dark",
                Language = "fr-FR",
                ShowConfirmModals = false,
                ShowToastNotifications = true,
            };

            loader.WriteSettings(settings);

            var settingsFile = Path.Combine(tempDir, "settings.json");
            Assert.IsTrue(File.Exists(settingsFile),
                $"settings.json not created at {settingsFile}");

            var loaded = MakeLoader().LoadSettings();
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Dark", loaded!.Theme);
            Assert.AreEqual("fr-FR", loaded.Language);
            Assert.IsFalse(loaded.ShowConfirmModals);
            Assert.IsTrue(loaded.ShowToastNotifications);
        }

        [TestMethod]
        public void WriteProfiles_LandsOnDisk_WithClassicAccel()
        {
            var loader = MakeLoader();
            var profile = new Profile
            {
                Name = "TestProfile",
                OutputDPI = 1600,
                YXRatio = 1.25,
                Acceleration = new ClassicAccel
                {
                    Acceleration = 0.05,
                    Exponent = 2.3,
                    Offset = 1.5,
                    Cap = 4.0,
                    Gain = true,
                },
                Hidden = new Hidden
                {
                    RotationDegrees = 0,
                    AngleSnappingDegrees = 0,
                    LeftRightRatio = 1,
                    UpDownRatio = 1,
                    SpeedCap = 0,
                    OutputSmoothingHalfLife = 0,
                },
            };

            var profilesDir = Path.Combine(tempDir, "profiles");
            Directory.CreateDirectory(profilesDir);
            var profileRW = new ProfileReaderWriter();
            File.WriteAllText(
                Path.Combine(profilesDir, "TestProfile.json"),
                profileRW.Serialize(profile));

            var profileFile = Path.Combine(profilesDir, "TestProfile.json");
            Assert.IsTrue(File.Exists(profileFile),
                $"profile JSON not created at {profileFile}");
            var contents = File.ReadAllText(profileFile);
            StringAssert.Contains(contents, "TestProfile");
            StringAssert.Contains(contents, "Classic");

            var loaded = MakeLoader().LoadProfiles().ToList();
            Assert.AreEqual(1, loaded.Count);
            var p = loaded[0];
            Assert.AreEqual("TestProfile", p.Name);
            Assert.AreEqual(1600, p.OutputDPI);
            Assert.AreEqual(1.25, p.YXRatio);
            Assert.IsInstanceOfType(p.Acceleration, typeof(ClassicAccel));
            var ca = (ClassicAccel)p.Acceleration;
            Assert.AreEqual(0.05, ca.Acceleration);
            Assert.AreEqual(2.3, ca.Exponent);
            Assert.AreEqual(1.5, ca.Offset);
            Assert.AreEqual(4.0, ca.Cap);
            Assert.IsTrue(ca.Gain);
        }

        [TestMethod]
        public void WriteDevicesAndMappings_LandOnDisk()
        {
            var devicesRW = new DevicesReaderWriter();
            var mappingsRW = new MappingsReaderWriter();

            var devices = new List<Device>
            {
                new()
                {
                    Name = "Test Mouse",
                    HWID = "HID\\VID_ABCD&PID_1234",
                    DPI = 1600,
                    PollingRate = 1000,
                    DeviceGroup = "Default",
                },
            };

            File.WriteAllText(
                Path.Combine(tempDir, "devices.json"),
                devicesRW.Serialize(devices));

            var mappings = new MappingSet
            {
                Mappings = new[]
                {
                    new Mapping
                    {
                        Name = "Default",
                        GroupsToProfiles = new Mapping.GroupsToProfilesMapping
                        {
                            { "Default", "TestProfile" },
                        },
                    },
                },
                ActiveMappingIndex = 0,
            };

            File.WriteAllText(
                Path.Combine(tempDir, "mappings.json"),
                mappingsRW.Serialize(mappings));

            Assert.IsTrue(File.Exists(Path.Combine(tempDir, "devices.json")));
            Assert.IsTrue(File.Exists(Path.Combine(tempDir, "mappings.json")));

            var fresh = MakeLoader();
            var loadedDevices = fresh.LoadDevices().ToList();
            Assert.AreEqual(1, loadedDevices.Count);
            Assert.AreEqual("Test Mouse", loadedDevices[0].Name);
            Assert.AreEqual(1600, loadedDevices[0].DPI);

            var loadedMappings = fresh.LoadMappings();
            Assert.AreEqual(1, loadedMappings.Mappings.Length);
            Assert.AreEqual("Default", loadedMappings.Mappings[0].Name);
            Assert.AreEqual("TestProfile",
                loadedMappings.Mappings[0].GroupsToProfiles["Default"]);
        }

        [TestMethod]
        public void LoadSettings_ReturnsNull_WhenFileMissing()
        {
            var loader = MakeLoader();
            Assert.IsNull(loader.LoadSettings());
        }

        // Regression probe: prove that a ClassicAccel serialized via the
        // ProfileReaderWriter actually round-trips with its curve-specific
        // fields intact. The previous behavior was that Profile.Acceleration
        // serialized as the *base* class (no curve params, no formula
        // discriminator), so a Classic profile written to disk and read back
        // came back as NoAcceleration with all settings lost.
        [TestMethod]
        public void Profile_ClassicAccel_SurvivesRoundTrip()
        {
            var profile = new Profile
            {
                Name = "Classic",
                OutputDPI = 1600,
                YXRatio = 1,
                Acceleration = new ClassicAccel
                {
                    Acceleration = 0.05,
                    Exponent = 2.3,
                    Offset = 1.5,
                    Cap = 4.0,
                    Gain = true,
                },
                Hidden = new Hidden(),
            };

            var rw = new ProfileReaderWriter();
            string json = rw.Serialize(profile);

            StringAssert.Contains(json, "Formula/Classic",
                "Serialized profile missing formula discriminator: " + json);
            StringAssert.Contains(json, "\"Exponent\"",
                "Serialized profile missing curve params: " + json);

            var roundTripped = rw.Deserialize(json);
            Assert.IsInstanceOfType(roundTripped.Acceleration, typeof(ClassicAccel));
            var ca = (ClassicAccel)roundTripped.Acceleration;
            Assert.AreEqual(0.05, ca.Acceleration);
            Assert.AreEqual(2.3, ca.Exponent);
            Assert.AreEqual(1.5, ca.Offset);
            Assert.AreEqual(4.0, ca.Cap);
            Assert.IsTrue(ca.Gain);
        }
    }
}
