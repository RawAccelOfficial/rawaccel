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
        /// Attempts to update the model directly. Validates the input as if it had been parsed from interface.
        /// This method should probably not be called from the interface. Instead, set InterfaceValue and
        /// call TryUpdateFromInterface().
        /// </summary>
        /// <param name="data">Value to which model should be tried to be set.</param>
        /// <returns>bool indicating success</returns>
        public bool TryUpdateModelDirectly(T data);
    }
}
