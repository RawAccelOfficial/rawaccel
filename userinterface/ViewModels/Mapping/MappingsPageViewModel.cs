using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Interfaces;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingsPageViewModel : ViewModelBase, IAsyncInitializable
    {
        private MappingViewModel? activeMappingView;
        private readonly BE.MappingsModel mappingsModel;
        private readonly IViewModelFactory viewModelFactory;
        private bool isInitialized = false;
        private bool isInitializing = false;

        public MappingsPageViewModel(userspace_backend.BackEnd backEnd, IViewModelFactory viewModelFactory)
        {
            mappingsModel = backEnd?.Mappings ?? throw new ArgumentNullException(nameof(backEnd));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

            MappingViews = [];
            UpdateMappingViews();
            mappingsModel.Mappings.CollectionChanged += MappingsCollectionChanged;

            AddMappingCommand = new RelayCommand(() => TryAddNewMapping());
        }

        private BE.MappingsModel MappingsBE => mappingsModel;

        public ObservableCollection<MappingViewModel> MappingViews { get; }

        public ICommand AddMappingCommand { get; }

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            UpdateMappingViews();

        public void UpdateMappingViews()
        {
            MappingViews.Clear();
            for (int i = 0; i < MappingsBE.Mappings.Count; i++)
            {
                var mappingBE = MappingsBE.Mappings[i];
                bool isActive = i == 0;

                var viewModel = viewModelFactory.CreateMappingViewModel(mappingBE, MappingsBE, isActive, OnMappingActivationRequested);

                MappingViews.Add(viewModel);

                if (isActive)
                {
                    activeMappingView = viewModel;
                }
            }
        }

        private void OnMappingActivationRequested(MappingViewModel requestingMapping)
        {
            SetActiveMapping(requestingMapping);
        }

        private void SetActiveMapping(MappingViewModel newActiveMapping)
        {
            if (activeMappingView != null)
            {
                activeMappingView.SetActiveState(false);
            }

            newActiveMapping.SetActiveState(true);
            activeMappingView = newActiveMapping;

            var activeIndex = MappingViews.IndexOf(newActiveMapping);
            if (activeIndex >= 0)
            {
                // TODO: Call backend method to set active mapping
            }
        }

        public bool TryAddNewMapping() => MappingsBE.TryAddMapping();

        public bool IsInitialized => isInitialized;
        public bool IsInitializing => isInitializing;

        public async Task InitializeAsync()
        {
            if (isInitialized || isInitializing)
                return;

            isInitializing = true;
            Console.WriteLine("MappingsPageViewModel.InitializeAsync called - Navigation to mappings page detected!");

            try
            {
                if (activeMappingView != null)
                {
                    Console.WriteLine($"Triggering animation for active mapping: {activeMappingView.MappingBE?.Name?.CurrentValidatedValue}");
                    activeMappingView.EnableAnimationAsync();
                }
                else
                {
                    Console.WriteLine("No active mapping found to animate");
                }

                await Task.Delay(100);
                isInitialized = true;
            }
            finally
            {
                isInitializing = false;
            }
        }
    }
}