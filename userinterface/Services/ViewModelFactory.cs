using System;
using userinterface.Services;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using BE = userspace_backend.Model;

namespace userinterface.Services
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly INotificationService notificationService;
        private readonly IThemeService themeService;

        public ViewModelFactory(INotificationService notificationService, IThemeService themeService)
        {
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        }

        public ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel)
        {
            return new ProfileViewModel(profileModel, notificationService, this);
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel)
        {
            return new ProfileSettingsViewModel(profileModel, notificationService);
        }

        public ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel)
        {
            return new ProfileChartViewModel(profileModel.XCurvePreview, profileModel.YCurvePreview, profileModel.YXRatio, themeService);
        }

        public ProfileListElementViewModel CreateProfileListElementViewModel(BE.ProfileModel profileModel, bool showButtons, bool isDefault)
        {
            return new ProfileListElementViewModel(profileModel, showButtons, isDefault);
        }

        public MappingViewModel CreateMappingViewModel(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested)
        {
            return new MappingViewModel(mappingModel, mappingsModel, isActive, onActivationRequested);
        }
    }
}
