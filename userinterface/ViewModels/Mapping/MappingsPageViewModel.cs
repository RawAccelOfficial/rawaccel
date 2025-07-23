using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingsPageViewModel : ViewModelBase
    {
        private MappingViewModel? activeMappingView;
        private readonly BE.MappingsModel mappingsModel;
        private readonly IViewModelFactory viewModelFactory;

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
    }
}