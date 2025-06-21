using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public EditableFieldViewModel Field { get; set; }

        public string Name => SettingBE.DisplayName;

        protected BE.IEditableSetting SettingBE { get; set; }
    }
}
