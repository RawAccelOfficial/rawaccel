using userinterface.Services;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.ProfileComponents;

namespace userinterface.ViewModels.Profile
{
    public partial class AnisotropyProfileSettingsViewModel : ViewModelBase
    {
        public AnisotropyProfileSettingsViewModel(BE.IAnisotropyModel anisotropyBE, LocalizationService localizationService)
        {
            AnisotropyBE = anisotropyBE;
            DomainX = new EditableFieldViewModel(AnisotropyBE.DomainX);
            DomainY = new EditableFieldViewModel(AnisotropyBE.DomainY);
            RangeX = new EditableFieldViewModel(AnisotropyBE.RangeX);
            RangeY = new EditableFieldViewModel(AnisotropyBE.RangeY);
            LPNorm = new NamedEditableFieldViewModel(AnisotropyBE.LPNorm, localizationService);
            // autoCommit so the toggle reaches the backend immediately; the chart
            // watches this to switch between one and two current-speed lines.
            CombineXY = new EditableBoolViewModel(AnisotropyBE.CombineXYComponents, localizationService, autoCommit: true);
        }

        protected BE.IAnisotropyModel AnisotropyBE { get; }

        public EditableFieldViewModel DomainX { get; set; }

        public EditableFieldViewModel DomainY { get; set; }

        public EditableFieldViewModel RangeX { get; set; }

        public EditableFieldViewModel RangeY { get; set; }

        public NamedEditableFieldViewModel LPNorm { get; set; }

        public EditableBoolViewModel CombineXY { get; set; }
    }
}