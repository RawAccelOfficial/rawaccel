using System.Collections.ObjectModel;
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
