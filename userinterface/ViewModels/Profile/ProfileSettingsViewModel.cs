using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileSettingsViewModel : ViewModelBase
    {
        public ProfileSettingsViewModel(BE.ProfileModel profileBE)
        {
            ProfileModelBE = profileBE;
            OutputDPIField = new NamedEditableFieldViewModel(profileBE.OutputDPI);
            YXRatioField = new NamedEditableFieldViewModel(profileBE.YXRatio);
            AccelerationSettings = new AccelerationProfileSettingsViewModel(profileBE.Acceleration);
            HiddenSettings = new HiddenProfileSettingsViewModel(profileBE.Hidden);
        }

        protected BE.ProfileModel ProfileModelBE { get; }

        public NamedEditableFieldViewModel OutputDPIField { get; set; }

        public NamedEditableFieldViewModel YXRatioField { get; set; }

        public AccelerationProfileSettingsViewModel AccelerationSettings { get; set; }

        public HiddenProfileSettingsViewModel HiddenSettings { get; set; }
    }
}
