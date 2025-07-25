using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace userspace_backend.Data
{
    public class Settings : INotifyPropertyChanged
    {
        private bool showToastNotifications = true;
        private string theme = "System";
        private bool autoSaveProfiles = true;
        private int saveIntervalMinutes = 5;
        private bool enableLogging = false;
        private string logLevel = "Info";
        private bool checkForUpdates = true;
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

        public bool AutoSaveProfiles
        {
            get => autoSaveProfiles;
            set
            {
                if (autoSaveProfiles != value)
                {
                    autoSaveProfiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SaveIntervalMinutes
        {
            get => saveIntervalMinutes;
            set
            {
                if (saveIntervalMinutes != value)
                {
                    saveIntervalMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableLogging
        {
            get => enableLogging;
            set
            {
                if (enableLogging != value)
                {
                    enableLogging = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LogLevel
        {
            get => logLevel;
            set
            {
                if (logLevel != value)
                {
                    logLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CheckForUpdates
        {
            get => checkForUpdates;
            set
            {
                if (checkForUpdates != value)
                {
                    checkForUpdates = value;
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