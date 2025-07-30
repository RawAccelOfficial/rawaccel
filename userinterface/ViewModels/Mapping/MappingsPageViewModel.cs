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

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateMappingViews(e);

        public void UpdateMappingViews(NotifyCollectionChangedEventArgs? e = null)
        {
            if (e == null)
            {
                RebuildAllMappingViews();
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    HandleMappingAdded(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    HandleMappingRemoved(e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    HandleMappingReplaced(e);
                    break;
                case NotifyCollectionChangedAction.Move:
                    HandleMappingMoved(e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                default:
                    RebuildAllMappingViews();
                    break;
            }

            UpdateActiveMappingReference();
        }

        private void RebuildAllMappingViews()
        {
            CleanupAllMappingViews();
            MappingViews.Clear();
            
            for (int i = 0; i < MappingsBE.Mappings.Count; i++)
            {
                var mappingBE = MappingsBE.Mappings[i];
                bool isActive = mappingBE.SetActive;

                var viewModel = viewModelFactory.CreateMappingViewModel(mappingBE, MappingsBE, isActive, OnMappingActivationRequested);
                MappingViews.Add(viewModel);
            }
        }

        private void HandleMappingAdded(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.NewStartingIndex < 0) return;

            for (int i = 0; i < e.NewItems.Count; i++)
            {
                if (e.NewItems[i] is not BE.MappingModel mappingBE) continue;

                int insertIndex = e.NewStartingIndex + i;
                bool isActive = mappingBE.SetActive;

                var viewModel = viewModelFactory.CreateMappingViewModel(mappingBE, MappingsBE, isActive, OnMappingActivationRequested);
                
                if (insertIndex >= MappingViews.Count)
                {
                    MappingViews.Add(viewModel);
                }
                else
                {
                    MappingViews.Insert(insertIndex, viewModel);
                }
            }
        }

        private void HandleMappingRemoved(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null || e.OldStartingIndex < 0) return;

            for (int i = e.OldItems.Count - 1; i >= 0; i--)
            {
                int removeIndex = e.OldStartingIndex + i;
                if (removeIndex >= 0 && removeIndex < MappingViews.Count)
                {
                    var viewModelToRemove = MappingViews[removeIndex];
                    viewModelToRemove.Cleanup();
                    MappingViews.RemoveAt(removeIndex);
                }
            }
        }

        private void HandleMappingReplaced(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.OldItems == null || e.NewStartingIndex < 0) return;

            for (int i = 0; i < Math.Min(e.NewItems.Count, e.OldItems.Count); i++)
            {
                int replaceIndex = e.NewStartingIndex + i;
                if (replaceIndex >= 0 && replaceIndex < MappingViews.Count)
                {
                    if (e.NewItems[i] is not BE.MappingModel mappingBE) continue;

                    var oldViewModel = MappingViews[replaceIndex];
                    oldViewModel.Cleanup();

                    bool isActive = mappingBE.SetActive;
                    var newViewModel = viewModelFactory.CreateMappingViewModel(mappingBE, MappingsBE, isActive, OnMappingActivationRequested);
                    
                    MappingViews[replaceIndex] = newViewModel;
                }
            }
        }

        private void HandleMappingMoved(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0) return;
            if (e.OldStartingIndex >= MappingViews.Count || e.NewStartingIndex >= MappingViews.Count) return;

            var viewModel = MappingViews[e.OldStartingIndex];
            MappingViews.RemoveAt(e.OldStartingIndex);
            MappingViews.Insert(e.NewStartingIndex, viewModel);
        }

        private void CleanupAllMappingViews()
        {
            foreach (var viewModel in MappingViews)
            {
                viewModel.Cleanup();
            }
        }

        private void UpdateActiveMappingReference()
        {
            activeMappingView = null;
            foreach (var viewModel in MappingViews)
            {
                if (viewModel.IsActiveMapping)
                {
                    activeMappingView = viewModel;
                    break;
                }
            }
        }

        private void OnMappingActivationRequested(MappingViewModel requestingMapping)
        {
            SetActiveMapping(requestingMapping);
        }

        private void SetActiveMapping(MappingViewModel newActiveMapping)
        {
            if (activeMappingView == newActiveMapping) return;
            
            if (MappingsBE.SetActiveMapping(newActiveMapping.MappingBE))
            {
                if (activeMappingView != null)
                {
                    activeMappingView.IsActiveMapping = false;
                }

                newActiveMapping.IsActiveMapping = true;
                activeMappingView = newActiveMapping;
            }
        }

        public bool TryAddNewMapping() => MappingsBE.TryAddMapping();

        public bool IsInitialized => isInitialized;
        public bool IsInitializing => isInitializing;

        public async Task InitializeAsync()
        {
            if (isInitialized || isInitializing) return;

            isInitializing = true;
            try
            {
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