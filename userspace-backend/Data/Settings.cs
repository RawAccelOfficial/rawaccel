using CommunityToolkit.Mvvm.ComponentModel;

namespace userspace_backend.Data
{
    public partial class Settings : ObservableObject
    {
        [ObservableProperty]
        private bool showToastNotifications = true;

        [ObservableProperty]
        private bool showConfirmModals = true;

        [ObservableProperty]
        private string theme = "System";

        [ObservableProperty]
        private string language = "en-US";
    }
}
