using System.Collections.ObjectModel;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ActiveProfilesListViewModel : ViewModelBase
    {
        public ActiveProfilesListViewModel()
        {
            ActiveProfiles = [];
            ActiveProfileItems = [];
        }

        public ObservableCollection<BE.ProfileModel> ActiveProfiles { get; }

        public ObservableCollection<ProfileListElementViewModel> ActiveProfileItems { get; set; }
    }
}