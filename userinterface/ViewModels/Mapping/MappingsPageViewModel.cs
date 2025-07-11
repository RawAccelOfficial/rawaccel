using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingsPageViewModel : ViewModelBase
    {
        private MappingViewModel? activeMappingView;

        public MappingsPageViewModel(BE.MappingsModel mappingsBE)
        {
            MappingsBE = mappingsBE;
            MappingViews = [];
            UpdateMappingViews();
            MappingsBE.Mappings.CollectionChanged += MappingsCollectionChanged;

            AddMappingCommand = new RelayCommand(() => TryAddNewMapping());
        }

        public BE.MappingsModel MappingsBE { get; }

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
                bool isActive = i == 0; // First mapping is active by default

                var viewModel = new MappingViewModel(mappingBE, MappingsBE, isActive, OnMappingActivationRequested);

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
                // Call backend method to set active mapping
                // MappingsBE.SetActiveMapping(activeIndex);
                // For now, we'll just handle it in the UI layer
            }
        }

        public bool TryAddNewMapping() => MappingsBE.TryAddMapping();
    }
}