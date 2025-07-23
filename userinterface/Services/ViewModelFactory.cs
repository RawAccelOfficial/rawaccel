using System;
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

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IServiceProvider ServiceProvider { get; }

        public ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = ServiceProvider.GetRequiredService<ProfileViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
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

    }
}
