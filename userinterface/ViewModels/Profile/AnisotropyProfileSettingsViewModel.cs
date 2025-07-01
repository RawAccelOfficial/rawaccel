using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.ProfileComponents;

namespace userinterface.ViewModels.Profile
{
    public partial class AnisotropyProfileSettingsViewModel : ViewModelBase
    {
        public AnisotropyProfileSettingsViewModel(BE.AnisotropyModel anisotropyBE)
        {
            AnisotropyBE = anisotropyBE;
            DomainX = new EditableFieldViewModel(AnisotropyBE.DomainX);
            DomainY = new EditableFieldViewModel(AnisotropyBE.DomainY);
            RangeX = new EditableFieldViewModel(AnisotropyBE.RangeX);
            RangeY = new EditableFieldViewModel(AnisotropyBE.RangeY);
            LPNorm = new NamedEditableFieldViewModel(AnisotropyBE.LPNorm);
        }

        protected BE.AnisotropyModel AnisotropyBE { get; }

        public EditableFieldViewModel DomainX { get; set; }

        public EditableFieldViewModel DomainY { get; set; }

        public EditableFieldViewModel RangeX { get; set; }

        public EditableFieldViewModel RangeY { get; set; }

        public NamedEditableFieldViewModel LPNorm { get; set; }
    }
}
