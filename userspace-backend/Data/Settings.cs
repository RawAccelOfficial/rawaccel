using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace userspace_backend.Data
{
    public class Settings : INotifyPropertyChanged
    {
        private bool showToastNotifications = true;
        private bool showConfirmModals = true;
        private string theme = "System";
        private string language = "en-US";

        public bool ShowToastNotifications
        {
            get => showToastNotifications;
            set
            {
                if (showToastNotifications != value)
                {
                    showToastNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Theme
        {
            get => theme;
            set
            {
                if (theme != value)
                {
                    theme = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowConfirmModals
        {
            get => showConfirmModals;
            set
            {
                if (showConfirmModals != value)
                {
                    showConfirmModals = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Language
        {
            get => language;
            set
            {
                if (language != value)
                {
                    language = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}