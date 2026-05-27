using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RawAccel.Contracts;
using userspace_backend.Driver;
using userspace_backend.Model;
using DATA = userspace_backend.Data;
using Profile = RawAccel.Contracts.RawAccelProfile;
using DeviceSettings = RawAccel.Contracts.RawAccelDeviceSettings;
using DeviceConfig = RawAccel.Contracts.RawAccelDeviceConfig;

namespace userspace_backend
{
    public interface IBackEnd
    {
        void Load();

        bool Apply();

        void SaveToDisk();

        void ImportSystemDevices();

        void ReloadSystemDevices();

        DevicesModel Devices { get; }

        MappingsModel Mappings { get; }

        IProfilesModel Profiles { get; }

        DATA.Settings Settings { get; }
    }

    public class BackEnd : IBackEnd
    {
        private readonly ILogger<BackEnd> logger;
        private readonly IRawAccelDriver driver;

        public BackEnd(
            IBackEndLoader backEndLoader,
            IRawAccelDriver driver,
            IProfilesModel profilesModel,
            DevicesModel devicesModel,
            MappingsModel mappingsModel,
            IServiceProvider serviceProvider,
            ILogger<BackEnd>? logger = null)
        {
            BackEndLoader = backEndLoader;
            this.driver = driver;
            Devices = devicesModel;
            Mappings = mappingsModel;
            Profiles = profilesModel;
            ServiceProvider = serviceProvider;
            this.logger = logger ?? NullLogger<BackEnd>.Instance;
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; } = null!;

        public IProfilesModel Profiles { get; set; }

        public DATA.Settings Settings { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        protected IServiceProvider ServiceProvider { get; set; }

        public void Load()
        {
            List<DATA.Device> devicesData = BackEndLoader.LoadDevices().ToList();
            Devices.TryMapFromData(devicesData);

            List<DATA.Profile> profilesData = BackEndLoader.LoadProfiles().ToList();
            Profiles.TryMapFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();

            RestoreDeviceGroupsFromData(devicesData, mappingData);

            LoadMappingsFromData(mappingData);

            Settings = BackEndLoader.LoadSettings() ?? new DATA.Settings();

            EnsureDefaultDeviceGroupExists();
            EnsureDefaultDeviceExists();
            EnsureDefaultProfileExists();
            EnsureDefaultMappingExists();
        }

        protected void RestoreDeviceGroupsFromData(
            IEnumerable<DATA.Device> devicesData,
            DATA.MappingSet mappingData)
        {
            foreach (DATA.Device device in devicesData)
            {
                if (!string.IsNullOrEmpty(device.DeviceGroup))
                {
                    Devices.DeviceGroups.AddOrGetDeviceGroup(device.DeviceGroup);
                }
            }

            foreach (DATA.Mapping mapping in mappingData?.Mappings ?? [])
            {
                foreach (string group in mapping.GroupsToProfiles.Keys)
                {
                    if (!string.IsNullOrEmpty(group))
                    {
                        Devices.DeviceGroups.AddOrGetDeviceGroup(group);
                    }
                }
            }
        }

        protected void LoadMappingsFromData(DATA.MappingSet mappingData)
        {
            // Clear existing mappings and reload
            Mappings.Mappings.Clear();
            foreach (var mapping in mappingData.Mappings)
            {
                Mappings.TryAddMapping(mapping);
            }
        }

        protected void EnsureDefaultDeviceGroupExists()
        {
            if (Devices.DeviceGroups.DeviceGroupModels.Count == 0)
            {
                Devices.DeviceGroups.AddOrGetDeviceGroup(DeviceGroups.DefaultDeviceGroup);
            }
        }

        protected void EnsureDefaultDeviceExists()
        {
            if (Devices.Elements.Count > 0)
            {
                return;
            }

            // When the OS reports connected input devices, skip the placeholder:
            // ImportSystemDevices will populate real devices instead.
            if (Devices.SystemDevices.SystemDevices.Count > 0)
            {
                return;
            }

            // TODO: This case is very niche, considering just not adding a
            // default at all to show that something is wrong.
            var defaultDevice = ServiceProvider.GetRequiredService<IDeviceModel>();
            defaultDevice.Name.TryUpdateModelDirectly("Default");
            defaultDevice.HardwareID.TryUpdateModelDirectly("DEFAULT_DEVICE_ID");
            defaultDevice.DeviceGroup.TryUpdateModelDirectly(DeviceGroups.DefaultDeviceGroup);
            
            Devices.TryInsert(0, defaultDevice);
        }

        public void ImportSystemDevices()
        {
            foreach (var systemDevice in Devices.SystemDevices.SystemDevices)
            {
                if (string.IsNullOrEmpty(systemDevice.HWID))
                {
                    continue;
                }

                // When reloading new devices list this will trigger
                bool alreadyPresent = Devices.Elements.Any(d =>
                    string.Equals(d.HardwareID.ModelValue, systemDevice.HWID, StringComparison.OrdinalIgnoreCase));
                if (alreadyPresent)
                {
                    continue;
                }

                var device = ServiceProvider.GetRequiredService<IDeviceModel>();
                device.Name.TryUpdateModelDirectly(systemDevice.Name);
                device.HardwareID.TryUpdateModelDirectly(systemDevice.HWID);
                device.DeviceGroup.TryUpdateModelDirectly(DeviceGroups.DefaultDeviceGroup);
                // DPI / PollRate / Use their DI-provided defaults.
                Devices.TryAdd(device);
            }
        }

        public void ReloadSystemDevices()
        {
            Devices.SystemDevices.RefreshSystemDevices();

            var connectedHwids = new HashSet<string>(
                Devices.SystemDevices.SystemDevices
                    .Select(sd => sd.HWID ?? string.Empty)
                    .Where(h => !string.IsNullOrEmpty(h)),
                StringComparer.OrdinalIgnoreCase);

            var toRemove = Devices.Elements
                .Where(d => !connectedHwids.Contains(d.HardwareID.ModelValue ?? string.Empty))
                .ToList();

            foreach (var device in toRemove)
            {
                Devices.TryRemoveElement(device);
            }

            ImportSystemDevices();
        }

        protected void EnsureDefaultProfileExists()
        {
            if (Profiles.Elements.Count == 0)
            {
                var defaultProfile = ServiceProvider.GetRequiredService<IProfileModel>();
                defaultProfile.Name.TryUpdateModelDirectly("Default");
                Profiles.TryInsert(0, defaultProfile);
            }
        }

        protected void EnsureDefaultMappingExists()
        {
            // Create a Default mapping when none exist at all (fresh install).
            if (Mappings.Mappings.Count == 0)
            {
                Mappings.TryAddMapping(new DATA.Mapping
                {
                    Name = "Default",
                    GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping(),
                });
            }

            // Self-heal: a Default mapping that exists but lacks the DefaultDeviceGroup
            // entry (e.g. a stale mappings.json with an empty GroupsToProfiles) must get
            // one. TryAddMapping is idempotent, so this no-ops when it is already mapped.
            if (Mappings.TryGetMapping("Default", out MappingModel? defaultMapping) && defaultMapping != null)
            {
                defaultMapping.TryAddMapping(DeviceGroups.DefaultDeviceGroup, "Default");
            }

            // Ensure at least one mapping is active.
            if (Mappings.GetMappingToSetActive() == null && Mappings.Mappings.Count > 0)
            {
                Mappings.Mappings[0].SetActive = true;
            }
        }

        public bool Apply()
        {
            logger.LogInformation("Apply clicked");

            MappingModel? mappingToApply = Mappings.GetMappingToSetActive();
            if (mappingToApply == null)
            {
                logger.LogError("Apply: Invalid state, no active mapping to apply");
                WriteSettingsToDisk();
                return false;
            }

            RawAccelConfig? config = null;
            try
            {
                config = MapToDriverConfig(mappingToApply);
                LogDriverConfigSummary(mappingToApply, config);
                LogDriverConfigJson(config);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Apply: error building RawAccelConfig");
            }

            bool driverApplied = false;
            if (config != null)
            {
                try
                {
                    driverApplied = driver.Apply(config);
                    if (driverApplied)
                    {
                        logger.LogInformation("Apply: driver.Apply() succeeded");
                    }
                    else
                    {
                        logger.LogError("Apply: driver.Apply() failed");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Apply: driver.Apply() threw");
                }
            }

            WriteSettingsToDisk();
            return driverApplied;
        }

        private void LogDriverConfigSummary(MappingModel mapping, RawAccelConfig config)
        {
            int profileCount = config.profiles?.Count ?? 0;
            int deviceCount = config.devices?.Count ?? 0;

            logger.LogInformation(
                "Apply: active mapping = {Mapping}, profiles = {ProfileCount}, devices = {DeviceCount}",
                mapping.Name?.ModelValue ?? "<unnamed>",
                profileCount,
                deviceCount);

            if (config.profiles != null)
            {
                foreach (Profile p in config.profiles)
                {
                    logger.LogInformation(
                        "  profile: name={Name} outputDPI={OutputDPI} yxRatio={YxRatio} rotation={Rotation} " +
                        "snap={Snap} inputSpeedCap={InputSpeedCap} accelModeX={AccelModeX} accelModeY={AccelModeY} " +
                        "accelX={AccelX}",
                        p.name, p.outputDPI, p.yxOutputDPIRatio, p.rotation, p.snap,
                        p.maximumSpeed, p.argsX.mode, p.argsY.mode, p.argsX.acceleration);
                }
            }

            if (config.devices != null)
            {
                foreach (DeviceSettings d in config.devices)
                {
                    logger.LogInformation(
                        "  device: id={Id} name={Name} profile={Profile} disable={Disable} dpi={Dpi} pollingRate={PollingRate}",
                        d.id, d.name, d.profile, d.config.disable, d.config.dpi, d.config.pollingRate);
                }
            }
        }

        private void LogDriverConfigJson(RawAccelConfig config)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    config,
                    Newtonsoft.Json.Formatting.Indented);
                logger.LogDebug("Apply: RawAccelConfig JSON{NewLine}{Json}", Environment.NewLine, json);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Apply: could not serialize RawAccelConfig to JSON");
            }
        }

        // TODO: These functions can be factored out later
        // Leave here for test/debug
        protected void WriteSettingsToDisk()
        {
            BackEndLoader.WriteSettingsToDisk(
                Devices.Elements,
                Mappings,
                Profiles.Elements);

            BackEndLoader.WriteSettings(Settings);
        }

        public void SaveToDisk()
        {
            try
            {
                logger.LogInformation("SaveToDisk requested (no driver write)");
                WriteSettingsToDisk();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SaveToDisk failed");
            }
        }

        protected RawAccelConfig MapToDriverConfig(MappingModel mappingModel)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(mappingModel);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(mappingModel);

            return new RawAccelConfig
            {
                version = RawAccelConstants.VersionString,
                defaultDeviceConfig = new DeviceConfig(),
                profiles = configProfiles.ToList(),
                devices = configDevices.ToList(),
            };
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(MappingModel mapping)
        {
            return mapping.IndividualMappings.SelectMany(
                dg => MapToDriverDevices(dg.DeviceGroup, dg.Profile.Name.ModelValue));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<IProfileModel> ProfilesToMap = mapping.IndividualMappings.Select(m => m.Profile).Distinct();
            return ProfilesToMap.Select(p => p.CurrentValidatedDriverProfile);
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(string dg, string profileName)
        {
            IEnumerable<IDeviceModel> deviceModels = Devices.Elements.Where(d => d.DeviceGroup.ModelValue.Equals(dg));
            return deviceModels.Select(dm => MapToDriverDevice(dm, profileName));
        }

        protected DeviceSettings MapToDriverDevice(IDeviceModel deviceModel, string profileName)
        {
            return new DeviceSettings()
            {
                id = deviceModel.HardwareID.ModelValue,
                name = deviceModel.Name.ModelValue,
                profile = profileName,
                config = new DeviceConfig()
                {
                    disable = deviceModel.Ignore.ModelValue,
                    dpi = deviceModel.DPI.ModelValue,
                    pollingRate = deviceModel.PollRate.ModelValue,
                    // Not yet surfaced in the UI/device model: these are the driver's
                    // expected defaults for poll-time clamping and extra-info passthrough.
                    // maximumTime/minimumTime bound the per-packet time delta (ms) the
                    // driver will trust. Keep in sync with the driver-side defaults if
                    // they ever become user-configurable.
                    pollTimeLock = false,
                    setExtraInfo = false,
                    maximumTime = 200,
                    minimumTime = 0.1,
                }
            };
        }
    }
}
