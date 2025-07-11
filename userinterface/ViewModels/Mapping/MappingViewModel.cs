using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingViewModel : ViewModelBase
    {
        private ObservableCollection<MappingListElementViewModel> mappingListElements;
        private bool isActiveMapping;
        private readonly Action<MappingViewModel>? onActivationRequested;

        public MappingViewModel(BE.MappingModel mappingBE, BE.MappingsModel mappingsBE, bool isDefault = false, Action<MappingViewModel>? onActivationRequested = null)
        {
            MappingBE = mappingBE;
            MappingsBE = mappingsBE;
            IsActiveMapping = isDefault;
            this.onActivationRequested = onActivationRequested;

            mappingListElements = new ObservableCollection<MappingListElementViewModel>();
            UpdateMappingListElements();

            MappingBE.IndividualMappings.CollectionChanged += (sender, e) =>
            {
                UpdateMappingListElements();
            };

            MappingBE.DeviceGroupsStillUnmapped.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(HasDeviceGroupsToAdd));
            };

            DeleteCommand = new RelayCommand(() => DeleteSelf());
            ActivateCommand = new RelayCommand(() => ActivateMapping(), () => !IsActiveMapping);
        }

        public BE.MappingModel MappingBE { get; }

        protected BE.MappingsModel MappingsBE { get; }

        public bool IsActiveMapping
        {
            get => isActiveMapping;
            set
            {
                if (SetProperty(ref isActiveMapping, value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BorderBrush));
                    OnPropertyChanged(nameof(BorderThickness));

                    // Update the ActivateCommand's can execute state
                    if (ActivateCommand is RelayCommand cmd)
                        cmd.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

        public ObservableCollection<MappingListElementViewModel> MappingListElements => mappingListElements;

        public bool HasDeviceGroupsToAdd => MappingBE.DeviceGroupsStillUnmapped.Any();

        public ICommand DeleteCommand { get; }

        public ICommand ActivateCommand { get; }

        public IBrush BorderBrush => IsActiveMapping ?
            new SolidColorBrush(Color.Parse("#22C55E")) :
            new SolidColorBrush(Color.Parse("#404040"));

        public Thickness BorderThickness => IsActiveMapping ?
            new Thickness(2) :
            new Thickness(1);

        private void UpdateMappingListElements()
        {
            foreach (var element in mappingListElements)
            {
                element.Cleanup();
            }

            mappingListElements.Clear();
            for (int i = 0; i < MappingBE.IndividualMappings.Count; i++)
            {
                var mappingGroup = MappingBE.IndividualMappings[i];
                // Consider the first mapping element as default
                bool isDefaultElement = IsActiveMapping && i == 0;
                mappingListElements.Add(new MappingListElementViewModel(mappingGroup, MappingBE, isDefaultElement));
            }
        }

        public void HandleAddMappingSelection(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is BE.DeviceGroupModel deviceGroup)
            {
                MappingBE.TryAddMapping(deviceGroup.CurrentValidatedValue, BE.ProfilesModel.DefaultProfile.CurrentNameForDisplay);
            }
        }

        private void ActivateMapping()
        {
            onActivationRequested?.Invoke(this);
        }

        public void SetActiveState(bool isActive)
        {
            IsActiveMapping = isActive;
        }

        public void DeleteSelf()
        {
            bool success = MappingsBE.RemoveMapping(MappingBE);
            Debug.Assert(success);
        }

        public void Cleanup()
        {
            foreach (var element in mappingListElements)
            {
                element.Cleanup();
            }
        }
    }
}