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
            IEnumerable<IDeviceModel> devices,
            MappingsModel mappings,
            IEnumerable<IProfileModel> profiles);

        public void WriteSettings(DATA.Settings settings);
    }

    public class BackEndLoader : IBackEndLoader
    {
        public BackEndLoader(
            string settingsDirectory,
            DevicesReaderWriter devicesReaderWriter,
            MappingsReaderWriter mappingsReaderWriter,
            ProfileReaderWriter profileReaderWriter,
            SettingsReaderWriter settingsReaderWriter)
        {
            SettingsDirectory = settingsDirectory;
            DevicesReaderWriter = devicesReaderWriter;
            MappingsReaderWriter = mappingsReaderWriter;
            ProfileReaderWriter = profileReaderWriter;
            SettingsReaderWriter = settingsReaderWriter;
        }

        public string SettingsDirectory { get; private set; }
        protected DevicesReaderWriter DevicesReaderWriter { get; }
        protected MappingsReaderWriter MappingsReaderWriter { get; }
        protected ProfileReaderWriter ProfileReaderWriter { get; }
        protected SettingsReaderWriter SettingsReaderWriter { get; }

        public IEnumerable<DATA.Device> LoadDevices()
        {
            string devicesFile = GetDevicesFile(SettingsDirectory);
            if (!File.Exists(devicesFile))
            {
                return [];
            }
            string devicesText = File.ReadAllText(devicesFile);
            return DevicesReaderWriter.Deserialize(devicesText) ?? [];
        }

        public DATA.MappingSet LoadMappings()
        {
            string mappingsFile = GetMappingsFile(SettingsDirectory);
            if (!File.Exists(mappingsFile))
            {
                return new DATA.MappingSet { Mappings = [] };
            }
            string mappingsText = File.ReadAllText(mappingsFile);
            return MappingsReaderWriter.Deserialize(mappingsText) ?? new DATA.MappingSet { Mappings = [] };
        }

        public IEnumerable<DATA.Profile> LoadProfiles()
        {
            string profilesDirectory = GetProfilesDirectory(SettingsDirectory);
            if (!Directory.Exists(profilesDirectory))
            {
                return [];
            }

            string[] profileFiles = Directory.GetFiles(profilesDirectory, "*.json");
            List<DATA.Profile> profiles = [];
            foreach (string profileFile in profileFiles)
            {
                string profileText = File.ReadAllText(profileFile);
                DATA.Profile? profileData = ProfileReaderWriter.Deserialize(profileText);
                if (profileData != null)
                {
                    profiles.Add(profileData);
                }
            }

            return profiles;
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
            IEnumerable<IDeviceModel> devices,
            MappingsModel mappings,
            IEnumerable<IProfileModel> profiles)
        {
            WriteDevices(devices);
            WriteMappings(mappings);
            WriteProfiles(profiles);
        }

        protected void WriteDevices(IEnumerable<IDeviceModel> devices)
        {
            IEnumerable<DATA.Device> devicesData = devices.Select(d => d.MapToData());
            string devicesFileText = DevicesReaderWriter.Serialize(devicesData);
            WriteFileAtomic(GetDevicesFile(SettingsDirectory), devicesFileText);
        }

        protected void WriteMappings(MappingsModel mappings)
        {
            DATA.MappingSet mappingsData = mappings.MapToData();
            string mappingsFileText = MappingsReaderWriter.Serialize(mappingsData);
            WriteFileAtomic(GetMappingsFile(SettingsDirectory), mappingsFileText);
        }

        protected void WriteProfiles(IEnumerable<IProfileModel> profiles)
        {
            string profilesDirectory = GetProfilesDirectory(SettingsDirectory);
            Directory.CreateDirectory(profilesDirectory);

            HashSet<string> writtenFiles = [];
            foreach (var profile in profiles)
            {
                DATA.Profile profileData = profile.MapToData();
                string profileFileText = ProfileReaderWriter.Serialize(profileData);
                string profileFilePath = GetProfileFile(profilesDirectory, profileData.Name);
                WriteFileAtomic(profileFilePath, profileFileText);
                writtenFiles.Add(profileFilePath);
            }

            // Remove profile files left behind by profiles that no longer exist.
            foreach (string existing in Directory.GetFiles(profilesDirectory, "*.json"))
            {
                if (!writtenFiles.Contains(existing))
                {
                    File.Delete(existing);
                }
            }
        }

        // Temp-then-rename so a crash mid-write can't corrupt the previous good file.
        private static void WriteFileAtomic(string path, string contents)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, contents);
            File.Move(tempPath, path, overwrite: true);
        }

        protected static string GetDevicesFile(string settingsDirectory) => Path.Combine(settingsDirectory, "devices.json");

        protected static string GetMappingsFile(string settingsDirectory) => Path.Combine(settingsDirectory, "mappings.json");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "profiles");

        protected static string GetProfileFile(string profileDirectory, string profileName) => Path.Combine(profileDirectory, $"{SanitizeFileName(profileName)}.json");

        // User-supplied profile names may contain filename-illegal chars
        // ('/', '\\', ':', ...); replace so the write doesn't throw. The on-disk
        // name isn't authoritative -- load reads Name from the file body.
        private static string SanitizeFileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }
            return name;
        }

        protected static string GetSettingsFile(string settingsDirectory) => Path.Combine(settingsDirectory, "settings.json");
    }
}
