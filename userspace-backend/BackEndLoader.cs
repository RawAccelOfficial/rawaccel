using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.IO;
using userspace_backend.Model;

namespace userspace_backend
{
    public interface IBackEndLoader
    {
        public IEnumerable<DATA.Device> LoadDevices();

        public DATA.MappingSet LoadMappings();

        public IEnumerable<DATA.Profile> LoadProfiles();

        public DATA.Settings? LoadSettings();

        public void WriteSettingsToDisk(
            IEnumerable<DeviceModel> devices,
            MappingsModel mappings,
            IEnumerable<ProfileModel> profiles);

        public void WriteSettings(DATA.Settings settings);
    }

    public class BackEndLoader : IBackEndLoader
    {
        public static DevicesReaderWriter DevicesReaderWriter = new DevicesReaderWriter();

        public static MappingsReaderWriter MappingsReaderWriter = new MappingsReaderWriter();

        public static ProfileReaderWriter ProfileReaderWriter = new ProfileReaderWriter();

        public static SettingsReaderWriter SettingsReaderWriter = new SettingsReaderWriter();

        public BackEndLoader(string settingsDirectory)
        {
            SettingsDirectory = settingsDirectory;
        }

        public string SettingsDirectory { get; private set; }

        public IEnumerable<DATA.Device> LoadDevices()
        {
            string devicesFile = GetDevicesFile(SettingsDirectory);
            string devicesText = File.ReadAllText(devicesFile);
            IEnumerable<DATA.Device> devicesData = DevicesReaderWriter.Read(devicesText);
            return devicesData;
        }

        public DATA.MappingSet LoadMappings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DATA.Profile> LoadProfiles()
        {
            throw new NotImplementedException();
        }

        public DATA.Settings? LoadSettings()
        {
            string settingsFile = GetSettingsFile(SettingsDirectory);
            
            if (!File.Exists(settingsFile))
            {
                return null;
            }

            try
            {
                return SettingsReaderWriter.Read(settingsFile);
            }
            catch
            {
                return null;
            }
        }

        public void WriteSettings(DATA.Settings settings)
        {
            string settingsFile = GetSettingsFile(SettingsDirectory);
            SettingsReaderWriter.Write(settingsFile, settings);
        }

        public void WriteSettingsToDisk(
            IEnumerable<DeviceModel> devices,
            MappingsModel mappings,
            IEnumerable<ProfileModel> profiles)
        {
            WriteDevices(devices);
            WriteMappings(mappings);
            WriteProfiles(profiles);
        }

        protected void WriteDevices(IEnumerable<DeviceModel> devices)
        {
            IEnumerable<DATA.Device> devicesData = devices.Select(d => d.MapToData());
            string devicesFileText = DevicesReaderWriter.Serialize(devicesData);
            string devicesFilePath = GetDevicesFile(SettingsDirectory);
            File.WriteAllText(devicesFilePath, devicesFileText);
        }

        protected void WriteMappings(MappingsModel mappings)
        {
            DATA.MappingSet mappingsData = mappings.MapToData();
            string mappingsFileText = MappingsReaderWriter.Serialize(mappingsData);
            string mappingsFilePath = GetMappingsFile(SettingsDirectory);
            File.WriteAllText(mappingsFilePath, mappingsFileText);
        }
        
        protected void WriteProfiles(IEnumerable<ProfileModel> profiles)
        {
            string profilesDirectory = GetProfilesDirectory(SettingsDirectory);
            Directory.CreateDirectory(profilesDirectory);

            foreach (var profile in profiles)
            {
                DATA.Profile profileData = profile.MapToData();
                string profileFileText = ProfileReaderWriter.Serialize(profileData);
                string profileFilePath = GetProfileFile(profilesDirectory, profileData.Name);
                File.WriteAllText(profileFilePath, profileFileText);
            }
        }

        protected static string GetDevicesFile(string settingsDirectory) => Path.Combine(settingsDirectory, "devices.json");

        protected static string GetMappingsFile(string settingsDirectory) => Path.Combine(settingsDirectory, "mappings.json");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "profiles");

        protected static string GetProfileFile(string profileDirectory, string profileName) => Path.Combine(profileDirectory, $"{profileName}.json");

        protected static string GetSettingsFile(string settingsDirectory) => Path.Combine(settingsDirectory, "settings.json");
    }
}
