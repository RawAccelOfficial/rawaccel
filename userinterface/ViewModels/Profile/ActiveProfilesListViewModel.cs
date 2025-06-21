using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ActiveProfilesListViewModel : ViewModelBase
    {
        public ActiveProfilesListViewModel()
        {
            ActiveProfiles = new ObservableCollection<BE.ProfileModel>();
        }

        public ObservableCollection<BE.ProfileModel> ActiveProfiles { get; }
    }
}
