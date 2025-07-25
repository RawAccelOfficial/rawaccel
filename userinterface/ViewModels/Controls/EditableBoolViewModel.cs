using CommunityToolkit.Mvvm.ComponentModel;
using userinterface.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class EditableBoolViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool valueInDisplay;

        private readonly LocalizationService localizationService;

        public EditableBoolViewModel(BE.IEditableSetting settingBE)
        {
            SettingBE = settingBE;
            localizationService = App.Services?.GetRequiredService<LocalizationService>()!;
            ResetValueFromBackEnd();
            
            // Subscribe to language changes to update the Name property
            if (localizationService != null)
            {
                localizationService.PropertyChanged += OnLanguageChanged;
            }
        }

        public string Name => GetLocalizedName();

        public bool Value => ValueInDisplay;

        protected BE.IEditableSetting SettingBE { get; }

        public bool TrySetFromInterface()
        {
            SettingBE.InterfaceValue = ValueInDisplay.ToString();
            bool wasSet = SettingBE.TryUpdateFromInterface();
            ResetValueFromBackEnd();
            return wasSet;
        }

        private void ResetValueFromBackEnd() =>
            ValueInDisplay = bool.TryParse(SettingBE.InterfaceValue, out bool result) && result;

        private string GetLocalizedName()
        {
            var displayText = SettingBE.DisplayText;
            
            // If there's a localization key, use the localization service to resolve it
            if (!string.IsNullOrEmpty(SettingBE.LocalizationKey))
            {
                return localizationService?.GetText(SettingBE.LocalizationKey) ?? displayText;
            }
            
            // Otherwise, use the display name directly (for user input settings)
            return displayText;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == LocalizationService.LanguageChangedPropertyName)
            {
                OnPropertyChanged(nameof(Name));
            }
        }

        partial void OnValueInDisplayChanged(bool value)
        {
            OnPropertyChanged(nameof(Value));
        }
    }
}