using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.Model;

namespace userspace_backend
{
    // TODO: remove before release
    public class Bootstrapper : IBackEndLoader
    {
        public DATA.Device[] DevicesToLoad { get; set; }

        public DATA.MappingSet MappingsToLoad { get; set; }

        public DATA.Profile[] ProfilesToLoad { get; set; }

        public DATA.Settings SettingsToLoad { get; set; }

        // Allows us to test parts of BackEndLoader as desired
        public BackEndLoader BackEndLoader { get; set; }

        public IEnumerable<DATA.Device> LoadDevices()
        {
            return DevicesToLoad;
        }

        public DATA.MappingSet LoadMappings()
        {
            return MappingsToLoad;
        }

        public IEnumerable<DATA.Profile> LoadProfiles()
        {
            return ProfilesToLoad;
        }

        public void WriteSettingsToDisk(
            IEnumerable<DeviceModel> devices,
            MappingsModel mappings,
            IEnumerable<ProfileModel> profiles)
        {
            BackEndLoader.WriteSettingsToDisk(devices, mappings, profiles);
        }

        public DATA.Settings? LoadSettings()
        {
            return SettingsToLoad ?? BackEndLoader?.LoadSettings();
        }

        public void WriteSettings(DATA.Settings settings)
        {
            BackEndLoader?.WriteSettings(settings);
        }
    }
}
