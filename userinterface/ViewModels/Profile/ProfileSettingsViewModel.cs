using userinterface.Services;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileSettingsViewModel : ViewModelBase
    {
        private readonly INotificationService notificationService;
        private readonly LocalizationService localizationService;

        public ProfileSettingsViewModel(INotificationService notificationService, LocalizationService localizationService)
        {
            this.notificationService = notificationService;
            this.localizationService = localizationService;
        }

        protected BE.ProfileModel ProfileModelBE { get; private set; } = null!;

        public EditableFieldViewModel OutputDPIField { get; private set; } = null!;

        public EditableFieldViewModel YXRatioField { get; private set; } = null!;

        public AccelerationProfileSettingsViewModel AccelerationSettings { get; private set; } = null!;

        public HiddenProfileSettingsViewModel HiddenSettings { get; private set; } = null!;

        public void Initialize(BE.ProfileModel profileModel)
        {
            ProfileModelBE = profileModel;
            OutputDPIField = new EditableFieldViewModel(profileModel.OutputDPI);
            YXRatioField = new EditableFieldViewModel(profileModel.YXRatio);
            AccelerationSettings = new AccelerationProfileSettingsViewModel(profileModel.Acceleration, notificationService, localizationService);
            HiddenSettings = new HiddenProfileSettingsViewModel(profileModel.Hidden);
        }
    }
}