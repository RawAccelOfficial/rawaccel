using System.ComponentModel;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSetting : INotifyPropertyChanged
    {
        string DisplayName { get; }

        string LocalizationKey { get; set; }

        string DisplayText { get; }

        string EditedValueForDiplay { get; }

        string InterfaceValue { get; set; }

        bool HasChanged();

        bool TryUpdateFromInterface();
    }
}
