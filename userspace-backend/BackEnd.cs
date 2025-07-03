using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.IO;
using userspace_backend.Model;
using DATA = userspace_backend.Data;

namespace userspace_backend
{
    public class BackEnd
    {
        public BackEnd(IBackEndLoader backEndLoader)
        {
            BackEndLoader = backEndLoader;
            Devices = new DevicesModel();
            Profiles = new ProfilesModel([]);
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; }

        public ProfilesModel Profiles { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        public void Load()
        {
            IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices(); ;
            LoadDevicesFromData(devicesData);

            IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles(); ;
            LoadProfilesFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
            Mappings = new MappingsModel(mappingData, Devices.DeviceGroups, Profiles);
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                Devices.TryAddDevice(deviceData);
            }
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            foreach (var profile in profileData)
            {
                Profiles.TryAddProfile(profile);
            }
        }

        public void Apply(ProfileModel profileModel)
        {
            try
            {
                WriteToDriver(profileModel);
            }
            catch (Exception ex)
            {
                return;
            }

            WriteSettingsToDisk();
        }

        protected void WriteSettingsToDisk()
        {
            BackEndLoader.WriteSettingsToDisk(
                Devices.DevicesEnumerable,
                Mappings,
                Profiles.Profiles);
        }

        protected void WriteToDriver(ProfileModel profileModel)
        {
            try
            {
                DriverConfig config = MapToDriverConfig(profileModel);
               
                string errors = config.Errors();
                if (errors != null)
                {
                    throw new Exception($"Config validation failed: {errors}");
                }

                try
                {
                    new Thread(() => {
                        try
                        {
                            config.Activate();
                        }
                        catch (Exception threadEx)
                        {
                            Console.WriteLine($"DEBUG: Exception in activation thread: {threadEx.Message}");
                            Console.WriteLine($"DEBUG: Stack trace: {threadEx.StackTrace}");
                        }
                    }).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Exception starting activation thread: {ex.Message}");
                    throw new Exception($"Driver activation failed: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in WriteToDriver: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // <=========================================================>
        // TODO: use the CurrentValidatedDriverProfile instead of this
        // <=========================================================>
        protected DriverConfig MapToDriverConfig(ProfileModel profileModel)
        {
            Profile customProfile = new Profile();

            customProfile.name = profileModel.Name.CurrentValidatedValue;
            customProfile.outputDPI = profileModel.OutputDPI.CurrentValidatedValue;
            customProfile.yxOutputDPIRatio = profileModel.YXRatio.CurrentValidatedValue;

            customProfile.lrOutputDPIRatio = 1.0;
            customProfile.udOutputDPIRatio = 1.0;
            customProfile.rotation = 0.0;
            customProfile.snap = 0.0;
            customProfile.maximumSpeed = 0.0; // 0 means no cap

            customProfile.domainXY = new Vec2<double> { x = 1.0, y = 1.0 };
            customProfile.rangeXY = new Vec2<double> { x = 1.0, y = 1.0 };

            DriverConfig config = DriverConfig.FromProfile(customProfile);

            return config;
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(MappingModel mapping)
        {
            return mapping.IndividualMappings.SelectMany(
                dg => MapToDriverDevices(dg.DeviceGroup, dg.Profile.Name.ModelValue));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<ProfileModel> ProfilesToMap = mapping.IndividualMappings.Select(m => m.Profile).Distinct();
            return ProfilesToMap.Select(p => p.CurrentValidatedDriverProfile);
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(DeviceGroupModel dg, string profileName)
        {
            IEnumerable<DeviceModel> deviceModels = Devices.Devices.Where(d => d.DeviceGroup.Equals(dg));
            return deviceModels.Select(dm => MapToDriverDevice(dm, profileName));
        }

        protected DeviceSettings MapToDriverDevice(DeviceModel deviceModel, string profileName)
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
