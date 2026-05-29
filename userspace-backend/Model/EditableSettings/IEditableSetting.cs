using System;
using System.ComponentModel;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSetting : INotifyPropertyChanged
    {
        string DisplayName { get; }

        string LocalizationKey { get; set; }

        string DisplayText { get; }

        string EditedValueForDisplay { get; }

        string InterfaceValue { get; set; }

        bool HasChanged();

        bool AutoUpdateFromInterface { get; set; }

        bool TryUpdateFromInterface();
    }

    public interface IEditableSettingSpecific<T> : IEditableSetting where T : IComparable
    {
        public T ModelValue { get; }

        public T CurrentValidatedValue { get; }

        /// <summary>
        /// Updates the model directly, validating as if parsed from the interface.
        /// Prefer setting InterfaceValue + TryUpdateFromInterface() from UI code.
        /// </summary>
        /// <returns>true on success.</returns>
        public bool TryUpdateModelDirectly(T data);
    }
}
