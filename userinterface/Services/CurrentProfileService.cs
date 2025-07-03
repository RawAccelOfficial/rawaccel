using System;
using BE = userspace_backend.Model;

namespace userinterface.Services
{
    public class CurrentProfileService
    {
        private BE.ProfileModel? _currentProfile;

        public BE.ProfileModel? CurrentProfile
        {
            get => _currentProfile;
            private set
            {
                if (_currentProfile != value)
                {
                    _currentProfile = value;
                    CurrentProfileChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<BE.ProfileModel?> CurrentProfileChanged;

        public void SetCurrentProfile(BE.ProfileModel? profile)
        {
            CurrentProfile = profile;
        }
    }
}
