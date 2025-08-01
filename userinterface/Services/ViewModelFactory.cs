using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using userinterface.Services;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
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
            var stopwatch = Stopwatch.StartNew();
            
            var viewModel = ServiceProvider.GetRequiredService<ProfileViewModel>();
            Debug.WriteLine($"ProfileViewModel service resolution: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            viewModel.Initialize(profileModel);
            Debug.WriteLine($"ProfileViewModel initialize: {stopwatch.ElapsedMilliseconds}ms");
            
            return viewModel;
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(BE.ProfileModel profileModel)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var viewModel = ServiceProvider.GetRequiredService<ProfileSettingsViewModel>();
            Debug.WriteLine($"ProfileSettingsViewModel service resolution: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            viewModel.Initialize(profileModel);
            Debug.WriteLine($"ProfileSettingsViewModel initialize: {stopwatch.ElapsedMilliseconds}ms");
            
            return viewModel;
        }

        public ProfileChartViewModel CreateProfileChartViewModel(BE.ProfileModel profileModel)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var viewModel = ServiceProvider.GetRequiredService<ProfileChartViewModel>();
            Debug.WriteLine($"ProfileChartViewModel service resolution: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            viewModel.Initialize(profileModel);
            Debug.WriteLine($"ProfileChartViewModel initialize: {stopwatch.ElapsedMilliseconds}ms");
            
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