using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using userinterface.Services;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class EditableBoolViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool valueInDisplay;

        private readonly LocalizationService localizationService;

        // When true, toggling the checkbox commits straight to the backend
        // setting (so dependent logic, e.g. the chart, reacts immediately).
        // Default false preserves the deferred-commit behavior other callers rely on.
        private readonly bool autoCommit;
        private bool suppressAutoCommit;

        public EditableBoolViewModel(BE.IEditableSetting settingBE, LocalizationService localizationService, bool autoCommit = false)
        {
            SettingBE = settingBE;
            this.localizationService = localizationService;
            this.autoCommit = autoCommit;
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

        private void ResetValueFromBackEnd()
        {
            // Suppress auto-commit while we mirror the backend value into the
            // display property, otherwise the resulting change event would
            // re-commit (and recurse).
            suppressAutoCommit = true;
            ValueInDisplay = bool.TryParse(SettingBE.InterfaceValue, out bool result) && result;
            suppressAutoCommit = false;
        }

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
            if (autoCommit && !suppressAutoCommit)
            {
                TrySetFromInterface();
            }
        }
    }
}