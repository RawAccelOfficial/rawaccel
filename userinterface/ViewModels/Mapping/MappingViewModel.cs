using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingViewModel : ViewModelBase
    {
        private ObservableCollection<MappingListElementViewModel> mappingListElements;
        private bool isActiveMapping;
        private bool animationEnabled = false;
        private Action<MappingViewModel>? onActivationRequested;

        public MappingViewModel()
        {
            mappingListElements = new ObservableCollection<MappingListElementViewModel>();
            DeleteCommand = new RelayCommand(() => DeleteSelf());
            ActivateCommand = new RelayCommand(() => ActivateMapping(), () => !IsActiveMapping);
        }

        public void Initialize(BE.MappingModel mappingModel, BE.MappingsModel mappingsModel, bool isActive, Action<MappingViewModel> onActivationRequested)
        {
            MappingBE = mappingModel;
            MappingsBE = mappingsModel;
            IsActiveMapping = isActive;
            this.onActivationRequested = onActivationRequested;

            UpdateMappingListElements();

            MappingBE.IndividualMappings.CollectionChanged += (sender, e) =>
            {
                UpdateMappingListElements();
            };

            MappingBE.DeviceGroupsStillUnmapped.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(HasDeviceGroupsToAdd));
            };
        }

        public BE.MappingModel MappingBE { get; private set; } = null!;

        protected BE.MappingsModel MappingsBE { get; private set; } = null!;

        public bool IsActiveMapping
        {
            get => isActiveMapping;
            set
            {
                if (SetProperty(ref isActiveMapping, value))
                {
                    if (value)
                    {
                        animationEnabled = true;
                    }
                    
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(SelectionBorderDashOffset));

                    if (ActivateCommand is RelayCommand cmd)
                        cmd.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsSelected => IsActiveMapping;

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

        public ObservableCollection<MappingListElementViewModel> MappingListElements => mappingListElements;

        public bool HasDeviceGroupsToAdd => MappingBE.DeviceGroupsStillUnmapped.Any();

        public ICommand DeleteCommand { get; }

        public ICommand ActivateCommand { get; }

        public string SelectionBorderPath
        {
            get
            {
                var cornerRadius = 8;
                var strokeWidth = 3;
                var borderThickness = 1;
                var padding = 16;
                
                var contentWidth = 400;
                var contentHeight = 350;
                
                var totalWidth = contentWidth + (2 * padding) + (2 * borderThickness);
                var totalHeight = contentHeight + (2 * padding) + (2 * borderThickness);
                
                var offset = strokeWidth / 2.0;
                var x = -offset;
                var y = -offset;
                var width = totalWidth + strokeWidth;
                var height = totalHeight + strokeWidth;
                
                return $"M {cornerRadius + x},{y} " +
                       $"L {width - cornerRadius + x},{y} " +
                       $"A {cornerRadius},{cornerRadius} 0 0,1 {width + x},{cornerRadius + y} " +
                       $"L {width + x},{height - cornerRadius + y} " +
                       $"A {cornerRadius},{cornerRadius} 0 0,1 {width - cornerRadius + x},{height + y} " +
                       $"L {cornerRadius + x},{height + y} " +
                       $"A {cornerRadius},{cornerRadius} 0 0,1 {x},{height - cornerRadius + y} " +
                       $"L {x},{cornerRadius + y} " +
                       $"A {cornerRadius},{cornerRadius} 0 0,1 {cornerRadius + x},{y} Z";
            }
        }

        public double SelectionBorderDashOffset => (IsActiveMapping && animationEnabled) ? 0 : 1640;


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

        public async void EnableAnimationAsync()
        {
            Console.WriteLine($"EnableAnimationAsync: Starting animation for {MappingBE?.Name?.CurrentValidatedValue}");
            await Task.Delay(100);
            animationEnabled = true;
            OnPropertyChanged(nameof(SelectionBorderDashOffset));
            Console.WriteLine($"EnableAnimationAsync: Animation triggered, DashOffset: {SelectionBorderDashOffset}");
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