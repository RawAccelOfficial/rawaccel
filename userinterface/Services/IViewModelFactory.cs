using System;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using BE = userspace_backend.Model;

namespace userinterface.Services
{
    public interface IViewModelFactory
    {
        ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel);
        ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel);
        ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel);
        MappingViewModel CreateMappingViewModel(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested);
        void ClearProfileViewModelCache();
        void RemoveProfileFromCache(string profileName);
    }
}
