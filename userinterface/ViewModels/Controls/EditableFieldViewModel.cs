using CommunityToolkit.Mvvm.ComponentModel;
using BE = userspace_backend.Model.EditableSettings;
using System.Diagnostics;

namespace userinterface.ViewModels.Controls
{
    public enum UpdateMode
    {
        LostFocus,
        OnChange
    }

    public partial class EditableFieldViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string valueText = string.Empty;

        public EditableFieldViewModel(BE.IEditableSetting settingBE, UpdateMode updateMode = UpdateMode.LostFocus)
        {
            SettingBE = settingBE;
            UpdateMode = updateMode;
            ResetValueTextFromBackEnd();
        }

        protected BE.IEditableSetting SettingBE { get; }

        public UpdateMode UpdateMode { get; set; }

        public bool TrySetFromInterface()
        {
            SettingBE.InterfaceValue = ValueText;
            bool wasSet = SettingBE.TryUpdateFromInterface();
            ResetValueTextFromBackEnd();
            return wasSet;
        }

        private void ResetValueTextFromBackEnd()
        {
            ValueText = SettingBE.InterfaceValue ?? string.Empty;
        }
    }
}
