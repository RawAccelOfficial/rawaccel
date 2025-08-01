using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using userinterface.Services;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls
{
    public partial class NamedEditableFieldViewModel : ViewModelBase
    {
        private readonly LocalizationService localizationService;

        public NamedEditableFieldViewModel(BE.IEditableSetting settingBE)
        {
            SettingBE = settingBE;
            Field = new EditableFieldViewModel(settingBE);
            localizationService = App.Services?.GetRequiredService<LocalizationService>()!;

            // Subscribe to language changes to update the Name property
            if (localizationService != null)
            {
                localizationService.PropertyChanged += OnLanguageChanged;
            }
        }

        public EditableFieldViewModel Field { get; }

        public string Name => GetLocalizedName();

        protected BE.IEditableSetting SettingBE { get; }

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
    }
}