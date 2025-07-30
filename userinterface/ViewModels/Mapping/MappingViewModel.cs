using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public static class BorderConstants
    {
        public const double DashLength = 1640;
        public const int CornerRadius = 8;
        public const int StrokeWidth = 3;
        public const int BorderThickness = 1;
        public const int Padding = 16;
        public const int ContentWidth = 400;
        public const int ContentHeight = 350;
    }

    public partial class MappingViewModel : ViewModelBase
    {
        private ObservableCollection<MappingListElementViewModel> mappingListElements;
        private bool isActiveMapping;
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

            MappingBE.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(BE.MappingModel.SetActive))
                {
                    IsActiveMapping = MappingBE.SetActive;
                }
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
                    OnPropertyChanged(nameof(SelectionBorderDashOffset));
                    (ActivateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<BE.MappingGroup> IndividualMappings => MappingBE.IndividualMappings;

        public ObservableCollection<MappingListElementViewModel> MappingListElements => mappingListElements;

        public bool HasDeviceGroupsToAdd => MappingBE.DeviceGroupsStillUnmapped.Any();

        public ICommand DeleteCommand { get; }

        public ICommand ActivateCommand { get; }

        public string SelectionBorderPath => GenerateSelectionBorderPath();

        private static string GenerateSelectionBorderPath()
        {
            var totalWidth = BorderConstants.ContentWidth + (2 * BorderConstants.Padding) + (2 * BorderConstants.BorderThickness);
            var totalHeight = BorderConstants.ContentHeight + (2 * BorderConstants.Padding) + (2 * BorderConstants.BorderThickness);
            
            var offset = BorderConstants.StrokeWidth / 2.0;
            var x = -offset;
            var y = -offset;
            var width = totalWidth + BorderConstants.StrokeWidth;
            var height = totalHeight + BorderConstants.StrokeWidth;
            var r = BorderConstants.CornerRadius;
            
            return $"M {r + x},{y} " +
                   $"L {width - r + x},{y} " +
                   $"A {r},{r} 0 0,1 {width + x},{r + y} " +
                   $"L {width + x},{height - r + y} " +
                   $"A {r},{r} 0 0,1 {width - r + x},{height + y} " +
                   $"L {r + x},{height + y} " +
                   $"A {r},{r} 0 0,1 {x},{height - r + y} " +
                   $"L {x},{r + y} " +
                   $"A {r},{r} 0 0,1 {r + x},{y} Z";
        }

        public double SelectionBorderDashOffset => IsActiveMapping ? 0 : 1640;
        private void UpdateMappingListElements()
        {
            mappingListElements.ToList().ForEach(element => element.Cleanup());
            mappingListElements.Clear();
            
            var elements = MappingBE.IndividualMappings
                .Select((mappingGroup, i) => new MappingListElementViewModel(mappingGroup, MappingBE))
                .ToList();
            
            foreach (var element in elements)
            {
                mappingListElements.Add(element);
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


        public void DeleteSelf()
        {
            if (!MappingsBE.RemoveMapping(MappingBE))
            {
                throw new InvalidOperationException("Failed to remove mapping");
            }
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