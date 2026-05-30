using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace userspace_backend.Model.EditableSettings
{
    public partial class EditableSettingV2<T> : ObservableObject, IEditableSettingSpecific<T> where T : IComparable
    {
        /// <summary>
        /// This value can be bound in UI for direct editing
        /// </summary>
        [ObservableProperty]
        public string interfaceValue;

        /// <summary>
        /// This value can be bound in UI for logic based on validated input
        /// </summary>
        [ObservableProperty]
        public T modelValue;

        public T CurrentValidatedValue => ModelValue;

        public const string LoggerCategoryName = "userspace_backend.EditableSetting";

        private readonly ILogger logger;

        public EditableSettingV2(
            string displayName,
            T initialValue,
            IUserInputParser<T> parser,
            IModelValueValidator<T> validator,
            bool autoUpdateFromInterface = false,
            string localizationKey = null,
            ILogger? logger = null)
        {
            DisplayName = displayName;
            LocalizationKey = localizationKey;
            LastWrittenValue = initialValue;
            Parser = parser;
            Validator = validator;
            UpdateModelValueFromLastKnown();
            SetInterfaceToModel();
            AutoUpdateFromInterface = autoUpdateFromInterface;
            this.logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Display name for this setting in UI
        /// </summary>
        public string DisplayName { get; }

        public string LocalizationKey { get; set; }

        public string DisplayText =>
            !string.IsNullOrEmpty(LocalizationKey)
                ? LocalizationKey
                : DisplayName ?? string.Empty;

        public string EditedValueForDisplay => InterfaceValue;

        public T LastWrittenValue { get; protected set; }

        /// <summary>
        /// Interface can set this for cases when new value arrives all at once (such as menu selection)
        /// instead of cases where new value arrives in parts (typing)
        /// </summary>
        public bool AutoUpdateFromInterface { get; set; }

        private IUserInputParser<T> Parser { get; }

        //TODO: change settings collections init so that this can be made private for non-static validators
        public IModelValueValidator<T> Validator { get; set; }

        private bool AllowAutoUpdateFromInterface { get; set; } = true;

        public bool HasChanged() => ModelValue.CompareTo(LastWrittenValue) != 0;

        public bool TryUpdateFromInterface()
        {
            bool result = TryUpdateFromInterfaceImpl(out bool editedInterfaceNeedsReset);

            if (editedInterfaceNeedsReset)
            {
                SetInterfaceToModel();
            }

            return result;
        }

        protected bool TryUpdateFromInterfaceImpl(out bool editedInterfaceNeedsReset)
        {
            editedInterfaceNeedsReset = true;

            if (string.IsNullOrEmpty(InterfaceValue))
            {
                logger.LogDebug(
                    "Setting '{Name}' ({Type}) commit skipped: InterfaceValue is empty",
                    DisplayName, typeof(T).Name);
                return false;
            }

            if (!Parser.TryParse(InterfaceValue.Trim(), out T parsedValue))
            {
                logger.LogDebug(
                    "Setting '{Name}' ({Type}) commit rejected: parser could not parse '{Value}'",
                    DisplayName, typeof(T).Name, InterfaceValue);
                return false;
            }

            return TryUpdateModelDirectlyImpl(parsedValue, out editedInterfaceNeedsReset);
        }

        protected void SetInterfaceToModel()
        {
            bool previous = AllowAutoUpdateFromInterface;
            AllowAutoUpdateFromInterface = false;
            InterfaceValue = ModelValue?.ToString();
            AllowAutoUpdateFromInterface = true;
        }

        protected void UpdateModelValueFromLastKnown()
        {
            UpdateModeValue(LastWrittenValue);
        }

        protected void UpdateModeValue(T value)
        {
            ModelValue = value;
        }

        partial void OnInterfaceValueChanged(string value)
        {
            // TODO: double-check race conditions
            if (AutoUpdateFromInterface && AllowAutoUpdateFromInterface)
            {
                TryUpdateFromInterface();
            }
        }

        // TODO: unit test
        public bool TryUpdateModelDirectly(T data)
        {
            return TryUpdateModelDirectlyImpl(data, out _);
        }

        private bool TryUpdateModelDirectlyImpl(T data, out bool editedInterfaceNeedsReset)
        {
            editedInterfaceNeedsReset = false;

            if (data.CompareTo(ModelValue) == 0)
            {
                return true;
            }

            if (!Validator.Validate(data))
            {
                editedInterfaceNeedsReset = true;
                logger.LogDebug(
                    "Setting '{Name}' ({Type}) rejected direct update: value {Value} failed validation",
                    DisplayName, typeof(T).Name, data);
                return false;
            }

            T previous = ModelValue;
            UpdateModeValue(data);
            SetInterfaceToModel();
            logger.LogDebug(
                "Setting '{Name}' ({Type}) changed: {Old} -> {New}",
                DisplayName, typeof(T).Name, previous, data);
            return true;
        }
    }
}
