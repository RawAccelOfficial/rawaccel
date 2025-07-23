using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using BE = userspace_backend.Model;

namespace userinterface.Services
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly ConcurrentDictionary<string, ProfileViewModel> _profileViewModelCache = new();

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IServiceProvider ServiceProvider { get; }

        public ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel)
        {
            var key = profileModel.Name.CurrentValidatedValue;
            return _profileViewModelCache.GetOrAdd(key, _ =>
            {
                var viewModel = ServiceProvider.GetRequiredService<ProfileViewModel>();
                viewModel.Initialize(profileModel);
                return viewModel;
            });
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = ServiceProvider.GetRequiredService<ProfileSettingsViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
        }

        public ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = ServiceProvider.GetRequiredService<ProfileChartViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
        }


        public MappingViewModel CreateMappingViewModel(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested)
        {
            var viewModel = ServiceProvider.GetRequiredService<MappingViewModel>();
            viewModel.Initialize(mappingModel, mappingsModel, isActive, onActivationRequested);
            return viewModel;
        }

        public void ClearProfileViewModelCache()
        {
            _profileViewModelCache.Clear();
        }

        public void RemoveProfileFromCache(string profileName)
        {
            _profileViewModelCache.TryRemove(profileName, out _);
        }
    }
}
