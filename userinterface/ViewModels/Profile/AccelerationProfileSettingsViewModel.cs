using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BE = userspace_backend.Model.AccelDefinitions;
using BEData = userspace_backend.Data.Profiles.Acceleration;

namespace userinterface.ViewModels.Profile
{
    public partial class AccelerationProfileSettingsViewModel : ViewModelBase
    {
        public static readonly ObservableCollection<string> DefinitionTypes =
            new(Enum.GetValues(typeof(BEData.AccelerationDefinitionType))
                .Cast<BEData.AccelerationDefinitionType>()
                .Select(d => d.ToString()));

        [ObservableProperty]
        public bool areAccelSettingsVisible;

        public AccelerationProfileSettingsViewModel(BE.AccelerationModel accelerationBE)
        {
            AccelerationBE = accelerationBE;
            AccelerationFormulaSettings = new AccelerationFormulaSettingsViewModel(accelerationBE.FormulaAccel);
            AccelerationLUTSettings = new AccelerationLUTSettingsViewModel(accelerationBE.LookupTableAccel);
            AnisotropySettings = new AnisotropyProfileSettingsViewModel(accelerationBE.Anisotropy);
            CoalescionSettings = new CoalescionProfileSettingsViewModel(accelerationBE.Coalescion);
            AccelerationBE.DefinitionType.AutoUpdateFromInterface = true;
            AccelerationBE.DefinitionType.PropertyChanged += OnDefinitionTypeChanged;
        }

        public BE.AccelerationModel AccelerationBE { get; }

        public static ObservableCollection<string> DefinitionTypesLocal => DefinitionTypes;

        public AccelerationFormulaSettingsViewModel AccelerationFormulaSettings { get; }

        public AccelerationLUTSettingsViewModel AccelerationLUTSettings { get; }

        public AnisotropyProfileSettingsViewModel AnisotropySettings { get; }

        public CoalescionProfileSettingsViewModel CoalescionSettings { get; }

        private void OnDefinitionTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccelerationBE.DefinitionType.CurrentValidatedValue))
            {
                AreAccelSettingsVisible = true;
            }
        }
    }
}