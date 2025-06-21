using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class EditableFieldViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string valueText;

        public EditableFieldViewModel(BE.IEditableSetting settingBE)
        {
            SettingBE = settingBE;
            ResetValueTextFromBackEnd();
        }

        protected BE.IEditableSetting SettingBE { get; set; }

        public bool TrySetFromInterface()
        {
            SettingBE.InterfaceValue = ValueText;
            bool wasSet = SettingBE.TryUpdateFromInterface();
            ResetValueTextFromBackEnd();
            return wasSet;
        }

        private void ResetValueTextFromBackEnd()
        {
            ValueText = SettingBE.InterfaceValue;
        }
    }
}
