using CommunityToolkit.Mvvm.ComponentModel;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class EditableBoolViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool valueInDisplay;

        public EditableBoolViewModel(BE.IEditableSetting settingBE)
        {
            SettingBE = settingBE;
            ResetValueFromBackEnd();
        }

        public string Name => SettingBE.DisplayName;

        protected BE.IEditableSetting SettingBE { get; }

        public bool TrySetFromInterface()
        {
            SettingBE.InterfaceValue = ValueInDisplay.ToString();
            bool wasSet = SettingBE.TryUpdateFromInterface();
            ResetValueFromBackEnd();
            return wasSet;
        }

        private void ResetValueFromBackEnd() => 
            ValueInDisplay = bool.TryParse(SettingBE.InterfaceValue, out bool result) && result;
    }
}
