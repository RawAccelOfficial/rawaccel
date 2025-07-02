using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class NamedEditableFieldViewModel : ViewModelBase
    {
        public NamedEditableFieldViewModel(BE.IEditableSetting settingBE)
        {
            SettingBE = settingBE;
            Field = new EditableFieldViewModel(settingBE);
        }

        public EditableFieldViewModel Field { get; }

        public string Name => SettingBE.DisplayName;

        protected BE.IEditableSetting SettingBE { get; }
    }
}
