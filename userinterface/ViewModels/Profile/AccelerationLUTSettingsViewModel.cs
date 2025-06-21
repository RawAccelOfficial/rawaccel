using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.AccelDefinitions;

namespace userinterface.ViewModels.Profile
{
    public partial class AccelerationLUTSettingsViewModel : ViewModelBase
    {
        public AccelerationLUTSettingsViewModel(BE.LookupTableDefinitionModel lutAccelBE)
        {
            LUTAccelBE = lutAccelBE;
            LUTPoints = new EditableFieldViewModel(lutAccelBE.Data);
        }

        public BE.LookupTableDefinitionModel LUTAccelBE { get; }

        public EditableFieldViewModel LUTPoints { get; set; }
    }
}
