using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RawAccel.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Driver;
using userspace_backend.IO;
using userspace_backend.Model;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.AccelDefinitions.Formula;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class BackEndApplyTests
    {
        private sealed class StubBackEndLoader : IBackEndLoader
        {
            public IEnumerable<DATA.Device> LoadDevices() => Array.Empty<DATA.Device>();

            public DATA.MappingSet LoadMappings() => new DATA.MappingSet
            {
                Mappings = Array.Empty<DATA.Mapping>(),
                ActiveMappingIndex = 0,
            };

            public IEnumerable<DATA.Profile> LoadProfiles() => Array.Empty<DATA.Profile>();

            public DATA.Settings? LoadSettings() => null;

            public void WriteSettingsToDisk(
                IEnumerable<IDeviceModel> devices,
                MappingsModel mappings,
                IEnumerable<IProfileModel> profiles)
            {
            }

            public void WriteSettings(DATA.Settings settings)
            {
            }
        }

        private sealed class StubSystemDevicesRetriever : ISystemDevicesRetriever
        {
            public IList<ISystemDevice> Devices { get; set; } = new List<ISystemDevice>();

            public IList<ISystemDevice> GetSystemDevices() => Devices;
        }

        private sealed class StubSystemDevice : ISystemDevice
        {
            public string Name { get; init; } = string.Empty;
            public string HWID { get; init; } = string.Empty;
        }

        // Captures whatever the BackEnd hands to its driver. Cross-platform:
        // implements IRawAccelDriver so the same tests run on Windows and Linux
        // builds without touching wrapper.dll or the agent socket.
        private sealed class CapturingDriver : IRawAccelDriver
        {
            public RawAccelConfig? CapturedConfig { get; private set; }
            public int ApplyCount { get; private set; }

            public bool IsAvailable => true;

            public bool Apply(RawAccelConfig config)
            {
                CapturedConfig = config;
                ApplyCount++;
                return true;
            }

            public RawAccelConfig Read() => CapturedConfig ?? new RawAccelConfig();

            public void Deactivate() { }

            public MouseSpeedSample GetCurrentMouseSpeedSample() => MouseSpeedSample.Zero;
        }

        private sealed class FakeAccelEvaluator : IAccelEvaluator
        {
            public IAccelInstance CreateInstance(RawAccelProfile profile) => new IdentityInstance();

            private sealed class IdentityInstance : IAccelInstance
            {
                public (double x, double y) Accelerate(
                    double x, double y, double dpiFactor, double timeMs) => (x, y);

                public void Dispose() { }
            }
        }

        private static (IBackEnd backEnd, CapturingDriver driver) BuildBackEndWithDefaults(
            IList<ISystemDevice>? systemDevices = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new StubBackEndLoader());
            // Register the stub BEFORE Compose; Compose uses TryAddSingleton so our stub wins.
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever
            {
                Devices = systemDevices ?? new List<ISystemDevice>(),
            });
            var driver = new CapturingDriver();
            services.AddSingleton<IRawAccelDriver>(driver);

            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();
            backEnd.Load();
            return (backEnd, driver);
        }

        private static RawAccelConfig ApplyAndCapture(IBackEnd backEnd, CapturingDriver driver)
        {
            backEnd.Apply();
            Assert.IsNotNull(driver.CapturedConfig, "Apply should have handed a RawAccelConfig to the driver.");
            return driver.CapturedConfig!;
        }

        [TestMethod]
        public void FormulaDIKeys_AreDistinctPerFormula_SoExponentDefaultsDoNotCollide()
        {
            // Regression: Power (and Jump) prefixed their DI keys with
            // nameof(ClassicAccelerationDefinitionModel), so Power.ExponentDIKey
            // equalled Classic.ExponentDIKey. AddEditableSetting uses
            // AddKeyedTransient (last registration wins), so Classic's Exponent
            // silently resolved to Power's default (0.05) instead of its own (2).
            Assert.AreNotEqual(
                ClassicAccelerationDefinitionModel.ExponentDIKey,
                PowerAccelerationDefinitionModel.ExponentDIKey,
                "Classic and Power exponent DI keys must be distinct.");
            Assert.AreNotEqual(
                ClassicAccelerationDefinitionModel.CapDIKey,
                PowerAccelerationDefinitionModel.CapDIKey,
                "Classic and Power cap DI keys must be distinct.");

            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new StubBackEndLoader());
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever());
            services.AddSingleton<IRawAccelDriver>(new CapturingDriver());
            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);

            var classicExponent = sp.GetRequiredKeyedService<IEditableSettingSpecific<double>>(
                ClassicAccelerationDefinitionModel.ExponentDIKey);
            var powerExponent = sp.GetRequiredKeyedService<IEditableSettingSpecific<double>>(
                PowerAccelerationDefinitionModel.ExponentDIKey);

            Assert.AreEqual(2.0, classicExponent.ModelValue, "Classic exponent default should be 2.");
            Assert.AreEqual(0.05, powerExponent.ModelValue, "Power exponent default should be 0.05.");
        }

        [TestMethod]
        public void RemovingReferencedProfile_ReassignsMappingToDefault()
        {
            var (backEnd, _) = BuildBackEndWithDefaults();

            Assert.IsTrue(backEnd.Profiles.TryAddNewDefaultProfile("Gaming"));
            backEnd.Devices.DeviceGroups.AddOrGetDeviceGroup("MyGroup");

            MappingModel mapping = backEnd.Mappings.GetActiveMapping()!;
            Assert.IsNotNull(mapping);
            Assert.IsTrue(mapping.TryAddMapping("MyGroup", "Gaming"));

            MappingGroup group = mapping.IndividualMappings.Single(g =>
                string.Equals(g.DeviceGroup, "MyGroup", StringComparison.InvariantCultureIgnoreCase));
            Assert.AreEqual("Gaming", group.Profile.Name.ModelValue);

            // Delete the referenced profile.
            Assert.IsTrue(backEnd.Profiles.TryGetProfile("Gaming", out IProfileModel? gaming) && gaming != null);
            Assert.IsTrue(backEnd.Profiles.RemoveProfile(gaming!));

            // The mapping entry must fall back to the default profile, not keep a dangling reference.
            Assert.AreEqual("Default", group.Profile.Name.ModelValue);
        }

        [TestMethod]
        public void RenamingProfileToExistingName_IsRejected_CaseInsensitive()
        {
            // ProfileNameValidator enforces uniqueness across profiles. A "Default"
            // profile already exists after Load.
            var (backEnd, _) = BuildBackEndWithDefaults();
            Assert.IsTrue(backEnd.Profiles.TryAddNewDefaultProfile("Gaming"));
            Assert.IsTrue(backEnd.Profiles.TryGetProfile("Gaming", out IProfileModel? gaming) && gaming != null);

            Assert.IsFalse(gaming!.Name.TryUpdateModelDirectly("Default"), "Renaming onto an existing name must be rejected.");
            Assert.IsFalse(gaming.Name.TryUpdateModelDirectly("default"), "Uniqueness must be case-insensitive.");
            Assert.AreEqual("Gaming", gaming.Name.ModelValue);

            // A genuinely unique name is still accepted.
            Assert.IsTrue(gaming.Name.TryUpdateModelDirectly("Gaming2"));
            Assert.AreEqual("Gaming2", gaming.Name.ModelValue);
        }

        [TestMethod]
        public void ProfileName_EmptyOrTooLong_IsRejected()
        {
            // ProfileNameValidator keeps the prior non-empty / max-length guard.
            var (backEnd, _) = BuildBackEndWithDefaults();
            Assert.IsTrue(backEnd.Profiles.TryAddNewDefaultProfile("Gaming"));
            Assert.IsTrue(backEnd.Profiles.TryGetProfile("Gaming", out IProfileModel? gaming) && gaming != null);

            Assert.IsFalse(gaming!.Name.TryUpdateModelDirectly(string.Empty), "Empty name must be rejected.");
            Assert.IsFalse(
                gaming.Name.TryUpdateModelDirectly(new string('a', MaxNameLengthValidator.MaxNameLength + 1)),
                "Name longer than the max length must be rejected.");
            Assert.AreEqual("Gaming", gaming.Name.ModelValue);
        }

        [TestMethod]
        public void EnsureDefaultMapping_FreshInstall_CreatesMappingWithDefaultEntry()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();

            Assert.IsTrue(
                backEnd.Mappings.TryGetMapping("Default", out MappingModel? mapping) && mapping != null,
                "A Default mapping should exist after Load on a fresh install.");
            Assert.AreEqual(
                1, mapping!.IndividualMappings.Count,
                "Fresh Default mapping must contain exactly one DefaultDeviceGroup -> Default entry.");
            Assert.AreEqual(DeviceGroups.DefaultDeviceGroup, mapping.IndividualMappings[0].DeviceGroup);
            Assert.AreEqual("Default", mapping.IndividualMappings[0].Profile.Name.ModelValue);

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(1, cfg.profiles.Count);
            Assert.AreEqual(1, cfg.devices.Count);
        }

        [TestMethod]
        public void EnsureDefaultMapping_StaleEmptyMapping_SelfHealsWithDefaultEntry()
        {
            // Simulate stale mappings.json: {"Mappings":[{"Name":"Default","GroupsToProfiles":{}}],"ActiveMappingIndex":0}
            var staleLoader = new StaleMappingBackEndLoader();
            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(staleLoader);
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever());
            var driver = new CapturingDriver();
            services.AddSingleton<IRawAccelDriver>(driver);
            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();
            backEnd.Load();

            Assert.IsTrue(
                backEnd.Mappings.TryGetMapping("Default", out MappingModel? mapping) && mapping != null,
                "Default mapping should still be present after loading stale empty state.");
            Assert.AreEqual(
                1, mapping!.IndividualMappings.Count,
                "Stale empty Default mapping must self-heal to one DefaultDeviceGroup -> Default entry.");

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(1, cfg.profiles.Count);
            Assert.AreEqual(1, cfg.devices.Count);
        }

        private sealed class StaleMappingBackEndLoader : IBackEndLoader
        {
            public IEnumerable<DATA.Device> LoadDevices() => Array.Empty<DATA.Device>();

            public DATA.MappingSet LoadMappings() => new DATA.MappingSet
            {
                Mappings = new[]
                {
                    new DATA.Mapping
                    {
                        Name = "Default",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping(),
                    },
                },
                ActiveMappingIndex = 0,
            };

            public IEnumerable<DATA.Profile> LoadProfiles() => Array.Empty<DATA.Profile>();
            public DATA.Settings? LoadSettings() => null;
            public void WriteSettingsToDisk(
                IEnumerable<IDeviceModel> devices,
                MappingsModel mappings,
                IEnumerable<IProfileModel> profiles) { }
            public void WriteSettings(DATA.Settings settings) { }
        }

        [TestMethod]
        public void Apply_DefaultState_ProducesOneProfileAndOneDevice()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var cfg = ApplyAndCapture(backEnd, driver);

            Assert.AreEqual(1, cfg.profiles.Count, "Expected exactly one profile in the DriverConfig.");
            Assert.AreEqual(1, cfg.devices.Count, "Expected exactly one device in the DriverConfig.");

            var device = cfg.devices[0];
            Assert.AreEqual("DEFAULT_DEVICE_ID", device.id);
            Assert.AreEqual(1000, device.config.dpi);
            Assert.AreEqual(1000, device.config.pollingRate);
        }

        [TestMethod]
        public void Apply_DefaultState_DeviceReferencesDefaultProfileByName()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var cfg = ApplyAndCapture(backEnd, driver);

            var device = cfg.devices[0];
            var profile = cfg.profiles[0];
            Assert.AreEqual(
                profile.name,
                device.profile,
                "Device.profile must match an existing Profile.name so the driver can resolve the mapping.");
        }

        [TestMethod]
        public void Apply_ProfileOutputDpiEdit_FlowsIntoDriverConfig()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var profile = backEnd.Profiles.Elements[0];

            Assert.IsTrue(profile.OutputDPI.TryUpdateModelDirectly(1600), "OutputDPI update should succeed.");

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(1600, cfg.profiles[0].outputDPI);
        }

        [TestMethod]
        public void Apply_DeviceDpiEdit_DoesNotAffectPollingRate()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var device = backEnd.Devices.Elements[0];

            Assert.IsTrue(device.DPI.TryUpdateModelDirectly(3200), "DPI update should succeed.");
            Assert.IsTrue(device.PollRate.TryUpdateModelDirectly(500), "PollRate update should succeed.");

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(3200, cfg.devices[0].config.dpi);
            Assert.AreEqual(500, cfg.devices[0].config.pollingRate);
        }

        [TestMethod]
        public void ImportSystemDevices_CreatesOneDevicePerSystemDevice()
        {
            var systemDevices = new List<ISystemDevice>
            {
                new StubSystemDevice { Name = "Logitech G Pro",    HWID = @"HID\VID_046D&PID_C54D&MI_00" },
                new StubSystemDevice { Name = "Razer DeathAdder",  HWID = @"HID\VID_1532&PID_0084" },
            };

            var (backEnd, _) = BuildBackEndWithDefaults(systemDevices);
            backEnd.ImportSystemDevices();

            // EnsureDefaultDeviceExists skipped the "Default" placeholder because system devices
            // were present, so the only devices in the list should be the imported ones.
            Assert.AreEqual(2, backEnd.Devices.Elements.Count);

            var imported = backEnd.Devices.Elements
                .Select(d => (d.Name.ModelValue, d.HardwareID.ModelValue))
                .ToList();
            CollectionAssert.Contains(imported, ("Logitech G Pro",   @"HID\VID_046D&PID_C54D&MI_00"));
            CollectionAssert.Contains(imported, ("Razer DeathAdder", @"HID\VID_1532&PID_0084"));

            foreach (var d in backEnd.Devices.Elements)
            {
                Assert.AreEqual(DeviceGroups.DefaultDeviceGroup, d.DeviceGroup.ModelValue,
                    "Imported devices should default to the Default device group.");
            }
        }

        [TestMethod]
        public void ImportSystemDevices_SkipsDevicesAlreadyPresentByHwid()
        {
            var systemDevices = new List<ISystemDevice>
            {
                new StubSystemDevice { Name = "Preloaded Mouse", HWID = @"HID\VID_9999&PID_0001" },
            };

            var (backEnd, _) = BuildBackEndWithDefaults(systemDevices);
            backEnd.ImportSystemDevices();
            Assert.AreEqual(1, backEnd.Devices.Elements.Count);

            // Re-import with the same system device — should be a no-op.
            backEnd.ImportSystemDevices();
            Assert.AreEqual(1, backEnd.Devices.Elements.Count,
                "Repeated ImportSystemDevices calls must not create duplicates when HWID already matches.");
        }

        [TestMethod]
        public void ReloadSystemDevices_RemovesDisconnectedAndAddsNew()
        {
            var initial = new List<ISystemDevice>
            {
                new StubSystemDevice { Name = "Mouse A", HWID = @"HID\VID_AAAA" },
                new StubSystemDevice { Name = "Mouse B", HWID = @"HID\VID_BBBB" },
            };

            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new StubBackEndLoader());
            var retrieverStub = new StubSystemDevicesRetriever { Devices = initial };
            services.AddSingleton<ISystemDevicesRetriever>(retrieverStub);
            services.AddSingleton<IRawAccelDriver>(new CapturingDriver());

            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();
            backEnd.Load();
            backEnd.ImportSystemDevices();
            Assert.AreEqual(2, backEnd.Devices.Elements.Count);

            // Simulate: Mouse A unplugged, Mouse C plugged in.
            retrieverStub.Devices = new List<ISystemDevice>
            {
                new StubSystemDevice { Name = "Mouse B", HWID = @"HID\VID_BBBB" },
                new StubSystemDevice { Name = "Mouse C", HWID = @"HID\VID_CCCC" },
            };

            backEnd.ReloadSystemDevices();

            var hwids = backEnd.Devices.Elements
                .Select(d => d.HardwareID.ModelValue)
                .ToList();
            CollectionAssert.AreEquivalent(
                new[] { @"HID\VID_BBBB", @"HID\VID_CCCC" },
                hwids);
        }

        [TestMethod]
        public void Apply_ProfileCurveCoefficientEdit_FlowsIntoDriverConfig()
        {
            // Regression for the "stale curve on Apply" bug: editing a coefficient inside
            // the currently-selected curve sub-model (Formula -> Classic -> Acceleration)
            // must propagate through EditableSettingsSelector.AnySettingChanged up to
            // ProfileModel.RecalculateDriverData so CurrentValidatedDriverProfile refreshes
            // before BackEnd.Apply() reads it via MapToDriverConfig.
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var profile = backEnd.Profiles.Elements[0];

            Assert.IsTrue(
                profile.Acceleration.DefinitionType.TryUpdateModelDirectly(
                    Acceleration.AccelerationDefinitionType.Formula),
                "Flipping DefinitionType to Formula should succeed.");

            var formulaAccel = (FormulaAccelModel)profile.Acceleration.GetSelectable(
                Acceleration.AccelerationDefinitionType.Formula);

            Assert.IsTrue(
                formulaAccel.FormulaType.TryUpdateModelDirectly(
                    FormulaAccel.AccelerationFormulaType.Classic),
                "Flipping FormulaType to Classic should succeed.");

            var classic = (ClassicAccelerationDefinitionModel)formulaAccel.GetSelectable(
                FormulaAccel.AccelerationFormulaType.Classic);

            const double expectedAcceleration = 0.123;
            Assert.IsTrue(
                classic.Acceleration.TryUpdateModelDirectly(expectedAcceleration),
                "Classic.Acceleration update should succeed.");

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(RawAccel.Contracts.AccelMode.classic, cfg.profiles[0].argsX.mode,
                "DriverConfig should reflect the chosen Classic formula.");
            Assert.AreEqual(expectedAcceleration, cfg.profiles[0].argsX.acceleration,
                "DriverConfig should reflect the tweaked Classic.Acceleration coefficient. " +
                "If this fails with the default coefficient, EditableSettingsSelector is not " +
                "propagating nested sub-model changes up to ProfileModel.");
        }

        [TestMethod]
        public void Apply_SingleCurve_PopulatesBothAxes()
        {
            // Regression: ProfileModel.MapToDriver used to set only argsX, leaving argsY
            // at its noaccel default. With the default by-component anisotropy mode
            // (CombineXYComponents == false) the native math indexes Y through argsY,
            // so vertical acceleration was silently dead while horizontal worked and
            // the flat sens multipliers still applied. The single model curve must
            // drive BOTH argsX and argsY.
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var profile = backEnd.Profiles.Elements[0];

            Assert.IsTrue(
                profile.Acceleration.DefinitionType.TryUpdateModelDirectly(
                    Acceleration.AccelerationDefinitionType.Formula),
                "Flipping DefinitionType to Formula should succeed.");

            var formulaAccel = (FormulaAccelModel)profile.Acceleration.GetSelectable(
                Acceleration.AccelerationDefinitionType.Formula);

            Assert.IsTrue(
                formulaAccel.FormulaType.TryUpdateModelDirectly(
                    FormulaAccel.AccelerationFormulaType.Classic),
                "Flipping FormulaType to Classic should succeed.");

            var classic = (ClassicAccelerationDefinitionModel)formulaAccel.GetSelectable(
                FormulaAccel.AccelerationFormulaType.Classic);

            const double expectedAcceleration = 0.123;
            Assert.IsTrue(
                classic.Acceleration.TryUpdateModelDirectly(expectedAcceleration),
                "Classic.Acceleration update should succeed.");

            var cfg = ApplyAndCapture(backEnd, driver);

            // X is the historically-tested axis; Y is the regression guard.
            Assert.AreEqual(RawAccel.Contracts.AccelMode.classic, cfg.profiles[0].argsY.mode,
                "argsY must carry the same accel mode as argsX, or vertical acceleration " +
                "is dead in by-component mode (argsY left at the noaccel default).");
            Assert.AreEqual(expectedAcceleration, cfg.profiles[0].argsY.acceleration,
                "argsY must carry the same curve coefficient as argsX.");
            Assert.AreEqual(cfg.profiles[0].argsX.mode, cfg.profiles[0].argsY.mode,
                "The single model curve must drive both axes identically.");
            Assert.AreEqual(cfg.profiles[0].argsX.acceleration, cfg.profiles[0].argsY.acceleration,
                "The single model curve must drive both axes identically.");
        }

        // Regression: a saved ClassicAccel used to StackOverflow on Load via
        // EditableSettingsSelectable.TryMapFromData recursing into itself.
        private sealed class ClassicAccelLoader : IBackEndLoader
        {
            public IEnumerable<DATA.Device> LoadDevices() => Array.Empty<DATA.Device>();
            public DATA.MappingSet LoadMappings() => new DATA.MappingSet
            {
                Mappings = Array.Empty<DATA.Mapping>(),
                ActiveMappingIndex = 0,
            };
            public IEnumerable<DATA.Profile> LoadProfiles() => new[]
            {
                new DATA.Profile
                {
                    Name = "Default",
                    OutputDPI = 1000,
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
                },
            };
            public DATA.Settings? LoadSettings() => null;
            public void WriteSettingsToDisk(
                IEnumerable<IDeviceModel> devices,
                MappingsModel mappings,
                IEnumerable<IProfileModel> profiles) { }
            public void WriteSettings(DATA.Settings settings) { }
        }

        [TestMethod]
        public void Load_ProfileWithClassicAccel_DoesNotRecurse()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new ClassicAccelLoader());
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever());
            services.AddSingleton<IRawAccelDriver>(new CapturingDriver());
            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();

            backEnd.Load();

            var profile = backEnd.Profiles.Elements.Single();
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.Formula,
                profile.Acceleration.DefinitionType.ModelValue);
            var formula = (FormulaAccelModel)profile.Acceleration.GetSelectable(
                Acceleration.AccelerationDefinitionType.Formula);
            Assert.AreEqual(FormulaAccel.AccelerationFormulaType.Classic,
                formula.FormulaType.ModelValue);
            var classic = (ClassicAccelerationDefinitionModel)formula.GetSelectable(
                FormulaAccel.AccelerationFormulaType.Classic);
            Assert.AreEqual(0.05, classic.Acceleration.ModelValue);
            Assert.AreEqual(2.3, classic.Exponent.ModelValue);
            Assert.AreEqual(1.5, classic.Offset.ModelValue);
            Assert.AreEqual(4.0, classic.Cap.ModelValue);
        }

        // Regression: an older AccelerationModel fallback wrote
        // Anisotropy.Domain={0,0} / Range={0,0} when the on-disk profile had a
        // missing Anisotropy block. Those zeros then round-tripped back to disk
        // and degenerated the preview curve to a flat line (domain=0 collapses
        // input speed to 0; range=0 collapses scale to 1). Loading a profile
        // with the legacy all-zero Anisotropy must sanitize back to identity
        // weights so the curve preview is meaningful.
        private sealed class ZeroAnisotropyLoader : IBackEndLoader
        {
            public IEnumerable<DATA.Device> LoadDevices() => Array.Empty<DATA.Device>();
            public DATA.MappingSet LoadMappings() => new DATA.MappingSet
            {
                Mappings = Array.Empty<DATA.Mapping>(),
                ActiveMappingIndex = 0,
            };
            public IEnumerable<DATA.Profile> LoadProfiles() => new[]
            {
                new DATA.Profile
                {
                    Name = "Default",
                    OutputDPI = 1000,
                    YXRatio = 1,
                    Acceleration = new ClassicAccel
                    {
                        Acceleration = 0.01,
                        Anisotropy = new Anisotropy
                        {
                            Domain = new Vector2 { X = 0, Y = 0 },
                            Range = new Vector2 { X = 0, Y = 0 },
                            LPNorm = 2,
                            CombineXYComponents = false,
                        },
                    },
                    Hidden = new Hidden(),
                },
            };
            public DATA.Settings? LoadSettings() => null;
            public void WriteSettingsToDisk(
                IEnumerable<IDeviceModel> devices,
                MappingsModel mappings,
                IEnumerable<IProfileModel> profiles) { }
            public void WriteSettings(DATA.Settings settings) { }
        }

        [TestMethod]
        public void Load_ProfileWithZeroAnisotropy_SanitizesToIdentityWeights()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new ZeroAnisotropyLoader());
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever());
            services.AddSingleton<IRawAccelDriver>(new CapturingDriver());
            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();

            backEnd.Load();

            var aniso = backEnd.Profiles.Elements.Single().Acceleration.Anisotropy;
            Assert.AreEqual(1.0, aniso.DomainX.ModelValue,
                "DomainX must be sanitized to identity; zero collapses input speed to 0.");
            Assert.AreEqual(1.0, aniso.DomainY.ModelValue);
            Assert.AreEqual(1.0, aniso.RangeX.ModelValue,
                "RangeX must be sanitized to identity; zero collapses curve scale to 1.");
            Assert.AreEqual(1.0, aniso.RangeY.ModelValue);
        }

        // Simulates the user-reported flow: app boots with a Default profile of
        // Type=None on disk, user switches DefinitionType to Formula then picks
        // Classic and edits a coefficient. The chained Apply must see the
        // Classic args in the RawAccelConfig the driver receives.
        [TestMethod]
        public void TypeChange_FromNoneToClassic_FlowsThroughApply()
        {
            var (backEnd, driver) = BuildBackEndWithDefaults();
            var profile = backEnd.Profiles.Elements[0];

            Assert.AreEqual(Acceleration.AccelerationDefinitionType.None,
                profile.Acceleration.DefinitionType.ModelValue,
                "Test precondition: fresh-install profile is Type=None.");

            Assert.IsTrue(profile.Acceleration.DefinitionType.TryUpdateModelDirectly(
                Acceleration.AccelerationDefinitionType.Formula));

            var formula = (FormulaAccelModel)profile.Acceleration.GetSelectable(
                Acceleration.AccelerationDefinitionType.Formula);

            Assert.IsTrue(formula.FormulaType.TryUpdateModelDirectly(
                FormulaAccel.AccelerationFormulaType.Classic));

            var classic = (ClassicAccelerationDefinitionModel)formula.GetSelectable(
                FormulaAccel.AccelerationFormulaType.Classic);
            Assert.IsTrue(classic.Acceleration.TryUpdateModelDirectly(0.42));

            var cfg = ApplyAndCapture(backEnd, driver);
            Assert.AreEqual(RawAccel.Contracts.AccelMode.classic, cfg.profiles[0].argsX.mode,
                "Apply must see Classic mode after the user-driven type change.");
            Assert.AreEqual(0.42, cfg.profiles[0].argsX.acceleration,
                "Apply must see the edited Classic coefficient, not stale state.");
        }

        // Regression: a user-created device group (e.g. "DeviceGroup0") was lost
        // on reload because DeviceGroups.DeviceGroupModels is the master list
        // backing the UI dropdown and MappingModel.TryAddMapping, but is never
        // rehydrated from devices.json or mappings.json. Symptoms:
        //   1. devices.json keeps device.DeviceGroup="DeviceGroup0" - this part
        //      survives, but the UI dropdown only shows "Default".
        //   2. mappings.json has DeviceGroup0 -> Some Profile, but TryAddMapping
        //      rejects the row because "DeviceGroup0" isn't registered.
        private sealed class CustomDeviceGroupLoader : IBackEndLoader
        {
            public IEnumerable<DATA.Device> LoadDevices() => new[]
            {
                new DATA.Device
                {
                    Name = "Mouse In Custom Group",
                    HWID = @"HID\VID_1111&PID_2222",
                    DPI = 1000,
                    PollingRate = 1000,
                    DeviceGroup = "DeviceGroup0",
                },
            };
            public DATA.MappingSet LoadMappings() => new DATA.MappingSet
            {
                Mappings = new[]
                {
                    new DATA.Mapping
                    {
                        Name = "Default",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping
                        {
                            { "Default", "Default" },
                            { "DeviceGroup0", "Default" },
                        },
                    },
                },
                ActiveMappingIndex = 0,
            };
            public IEnumerable<DATA.Profile> LoadProfiles() => new[]
            {
                new DATA.Profile
                {
                    Name = "Default",
                    OutputDPI = 1000,
                    YXRatio = 1,
                    Acceleration = new userspace_backend.Data.Profiles.Accel.NoAcceleration(),
                    Hidden = new Hidden(),
                },
            };
            public DATA.Settings? LoadSettings() => null;
            public void WriteSettingsToDisk(
                IEnumerable<IDeviceModel> devices,
                MappingsModel mappings,
                IEnumerable<IProfileModel> profiles) { }
            public void WriteSettings(DATA.Settings settings) { }
        }

        [TestMethod]
        public void Load_CustomDeviceGroupInDevicesAndMappings_RestoresGroupList()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackEndLoader>(new CustomDeviceGroupLoader());
            services.AddSingleton<ISystemDevicesRetriever>(new StubSystemDevicesRetriever());
            services.AddSingleton<IRawAccelDriver>(new CapturingDriver());
            services.AddSingleton<IAccelEvaluator>(new FakeAccelEvaluator());
            var sp = BackEndComposer.Compose(services);
            var backEnd = sp.GetRequiredService<IBackEnd>();

            backEnd.Load();

            CollectionAssert.Contains(
                backEnd.Devices.DeviceGroups.DeviceGroupModels,
                "DeviceGroup0",
                "DeviceGroup0 should be restored into the master list from devices.json / mappings.json.");
            CollectionAssert.Contains(
                backEnd.Devices.DeviceGroups.DeviceGroupModels,
                DeviceGroups.DefaultDeviceGroup);

            Assert.IsTrue(
                backEnd.Mappings.TryGetMapping("Default", out MappingModel? mapping) && mapping != null);
            var groupNames = mapping!.IndividualMappings.Select(m => m.DeviceGroup).ToList();
            CollectionAssert.Contains(groupNames, "DeviceGroup0",
                "MappingModel must keep the DeviceGroup0 row after Load (was silently dropped before restore).");
            CollectionAssert.Contains(groupNames, DeviceGroups.DefaultDeviceGroup);
        }

        [TestMethod]
        public void ImportSystemDevices_SyncsInterfaceValueSoUiReflectsRealValues()
        {
            // Regression: EditableSettingV2.TryUpdateModelDirectly used to update ModelValue
            // but not InterfaceValue. The UI binds to InterfaceValue via EditableFieldViewModel,
            // so imported devices showed the DI placeholder ("name", "hwid") even though
            // ModelValue was correct. Guard against that by asserting both properties update.
            var systemDevices = new List<ISystemDevice>
            {
                new StubSystemDevice { Name = "RealMouseName", HWID = @"HID\VID_1234&PID_5678" },
            };

            var (backEnd, _) = BuildBackEndWithDefaults(systemDevices);
            backEnd.ImportSystemDevices();

            var imported = backEnd.Devices.Elements.Single();
            Assert.AreEqual("RealMouseName",                   imported.Name.ModelValue);
            Assert.AreEqual("RealMouseName",                   imported.Name.InterfaceValue,
                "InterfaceValue must mirror ModelValue so the UI shows the imported Name.");
            Assert.AreEqual(@"HID\VID_1234&PID_5678",          imported.HardwareID.ModelValue);
            Assert.AreEqual(@"HID\VID_1234&PID_5678",          imported.HardwareID.InterfaceValue,
                "InterfaceValue must mirror ModelValue so the UI shows the imported HWID.");
        }
    }
}
