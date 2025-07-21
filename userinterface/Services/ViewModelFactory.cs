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
        private readonly IServiceProvider serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ProfileViewModel CreateProfileViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = serviceProvider.GetRequiredService<ProfileViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = serviceProvider.GetRequiredService<ProfileSettingsViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
        }

        public ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel)
        {
            var viewModel = serviceProvider.GetRequiredService<ProfileChartViewModel>();
            viewModel.Initialize(profileModel);
            return viewModel;
        }

        public ProfileListElementViewModel CreateProfileListElementViewModel(BE.ProfileModel profileModel, bool showButtons, bool isDefault)
        {
            var viewModel = serviceProvider.GetRequiredService<ProfileListElementViewModel>();
            viewModel.Initialize(profileModel, showButtons, isDefault);
            return viewModel;
        }

        public MappingViewModel CreateMappingViewModel(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested)
        {
            var viewModel = serviceProvider.GetRequiredService<MappingViewModel>();
            viewModel.Initialize(mappingModel, mappingsModel, isActive, onActivationRequested);
            return viewModel;
        }
    }
}
