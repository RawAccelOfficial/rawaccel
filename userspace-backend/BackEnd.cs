using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using userspace_backend.Data.Profiles;
using userspace_backend.IO;
using userspace_backend.Model;
using DATA = userspace_backend.Data;

namespace userspace_backend
{
    public interface IBackEnd
    {
        void Load();

        void Apply();

        void WriteSettings();

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
        private readonly IDriverConfigActivator driverConfigActivator;

        public BackEnd(
            IBackEndLoader backEndLoader,
            IDriverConfigActivator driverConfigActivator,
            IProfilesModel profilesModel,
            DevicesModel devicesModel,
            MappingsModel mappingsModel,
            IServiceProvider serviceProvider,
            ILogger<BackEnd>? logger = null)
        {
            BackEndLoader = backEndLoader;
            this.driverConfigActivator = driverConfigActivator;
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
            IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices();
            LoadDevicesFromData(devicesData);

            IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles();
            LoadProfilesFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
            LoadMappingsFromData(mappingData);

            Settings = BackEndLoader.LoadSettings() ?? new DATA.Settings();

            EnsureDefaultDeviceGroupExists();
            EnsureDefaultDeviceExists();
            EnsureDefaultProfileExists();
            EnsureDefaultMappingExists();
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            Devices.TryMapFromData(devicesData);
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            Profiles.TryMapFromData(profileData);
        }

        protected void LoadMappingsFromData(DATA.MappingSet mappingData)
        {
            // Clear existing mappings and reload from data
            Mappings.Mappings.Clear();
            foreach (var mapping in mappingData.Mappings)
            {
                Mappings.TryAddMapping(mapping);
            }
        }

        protected void EnsureDefaultDeviceGroupExists()
        {
            // If no device groups exist, create a "Default" group
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

            var defaultDevice = ServiceProvider.GetRequiredService<IDeviceModel>();
            defaultDevice.Name.TryUpdateModelDirectly("Default");
            defaultDevice.HardwareID.TryUpdateModelDirectly("DEFAULT_DEVICE_ID");
            defaultDevice.DeviceGroup.TryUpdateModelDirectly(DeviceGroups.DefaultDeviceGroup);
            // DPI, PollRate, and Ignore already have sensible defaults from DI (1000, 1000, false)

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
                // DPI / PollRate / Ignore keep their DI-provided defaults.
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
            // If no profiles exist, create a default profile
            if (Profiles.Elements.Count == 0)
            {
                var defaultProfile = ServiceProvider.GetRequiredService<IProfileModel>();
                defaultProfile.Name.TryUpdateModelDirectly("Default");
                Profiles.TryInsert(0, defaultProfile);
            }
        }

        protected void EnsureDefaultMappingExists()
        {
            // Ensure a Default mapping object exists in the list.
            if (!Mappings.TryGetMapping("Default", out _))
            {
                Mappings.TryAddMapping(new DATA.Mapping
                {
                    Name = "Default",
                    GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping(),
                });
            }

            // Explicitly wire the DefaultDeviceGroup to "Default" profile entry.
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

        public void Apply()
        {
            logger.LogInformation("Apply clicked");

            MappingModel? mappingToApply = Mappings.GetMappingToSetActive();
            if (mappingToApply == null)
            {
                logger.LogWarning("Apply: no active mapping to apply");
                WriteSettingsToDisk();
                return;
            }

            DriverConfig? config = null;
            try
            {
                config = MapToDriverConfig(mappingToApply);
                LogDriverConfigSummary(mappingToApply, config);
                LogDriverConfigJson(config);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Apply: error building DriverConfig");
            }

            if (config != null)
            {
                try
                {
                    driverConfigActivator.Write(config);
                    logger.LogInformation("Apply: driver.Activate() succeeded");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Apply: driver.Activate() failed");
                }
            }

            WriteSettingsToDisk();
        }

        public void WriteSettings()
        {
            BackEndLoader.WriteSettings(Settings);
        }

        private void LogDriverConfigSummary(MappingModel mapping, DriverConfig config)
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

        private void LogDriverConfigJson(DriverConfig config)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    config,
                    Newtonsoft.Json.Formatting.Indented);
                logger.LogDebug("Apply: DriverConfig JSON{NewLine}{Json}", Environment.NewLine, json);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Apply: could not serialize DriverConfig to JSON");
            }
        }

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

        protected DriverConfig MapToDriverConfig(MappingModel mappingModel)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(mappingModel);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(mappingModel);

            DriverConfig config = DriverConfig.GetDefault();
            config.profiles = configProfiles.ToList();
            config.devices = configDevices.ToList();
            config.accels = configProfiles.Select(p => new ManagedAccel(p)).ToList();
            return config;
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
                    pollTimeLock = false,
                    setExtraInfo = false,
                    maximumTime = 200,
                    minimumTime = 0.1,
                }
            };
        }
    }
}
