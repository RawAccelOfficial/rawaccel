using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    /// <summary>
    /// Internal node of the settings tree.
    /// </summary>
    /// <remarks>
    /// Settings form a tree: the root model is the root node, collections are internal
    /// nodes (this interface), and individual settings are leaves. Collections may hold
    /// other collections and/or settings.
    /// </remarks>
    public interface IEditableSettingsCollectionV2
    {
        public bool HasChanged { get; }

        public EventHandler AnySettingChanged { get; set; }
    }

    public interface IEditableSettingsCollectionSpecific<T> : IEditableSettingsCollectionV2
    {
        T MapToData();

        bool TryMapFromData(T data);
    }

    public interface INamedEditableSettingsCollectionSpecific<T> : IEditableSettingsCollectionSpecific<T>
    {
        public IEditableSettingSpecific<string> Name { get; }
    }

    /// <summary>
    /// Shared base for settings-collection internal nodes. Holds change tracking
    /// and event plumbing common to <see cref="EditableSettingsCollection{T}"/>
    /// and <see cref="EditableSettingsCollectionV2{T}"/>. Subclasses populate
    /// <see cref="AllContainedEditableSettings"/> and
    /// <see cref="AllContainedEditableSettingsCollections"/> and wire the change handlers.
    /// </summary>
    public abstract class EditableSettingsCollectionBase<T> : ObservableObject, IEditableSettingsCollectionV2
    {
        public EventHandler AnySettingChanged { get; set; }

        public IEnumerable<IEditableSetting> AllContainedEditableSettings { get; protected set; }

        public IEnumerable<IEditableSettingsCollectionV2> AllContainedEditableSettingsCollections { get; protected set; }

        public bool HasChanged { get; protected set; }

        public void EvaluateWhetherHasChanged()
        {
            HasChanged = AllContainedEditableSettings.Any(s => s.HasChanged())
                || AllContainedEditableSettingsCollections.Any(c => c.HasChanged);
        }

        protected void EditableSettingChangedEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(IEditableSettingSpecific<IComparable>.ModelValue)))
            {
                OnAnySettingChanged();
            }
        }

        protected void EditableSettingsCollectionChangedEventHandler(object? sender, EventArgs e)
        {
            OnAnySettingChanged();
        }

        protected void OnAnySettingChanged()
        {
            AnySettingChanged?.Invoke(this, new EventArgs());
        }

        public abstract T MapToData();
    }

    public abstract class EditableSettingsCollection<T> : EditableSettingsCollectionBase<T>
    {
        public EditableSettingsCollection(T dataObject)
        {
            InitEditableSettingsAndCollections(dataObject);
            GatherEditableSettings();
            GatherEditableSettingsCollections();
        }

        public void GatherEditableSettings()
        {
            AllContainedEditableSettings = EnumerateEditableSettings();

            foreach (var setting in AllContainedEditableSettings)
            {
                // TODO: revisit settings composition so that this null check is unnecessary
                if (setting != null)
                {
                    setting.PropertyChanged += EditableSettingChangedEventHandler;
                }
            }
        }
        public void GatherEditableSettingsCollections()
        {
            AllContainedEditableSettingsCollections = EnumerateEditableSettingsCollections();

            // TODO: separate "All" and "currently selected" settings collections
            // so that incorrect assignment is not done here for collections that alter this through use
            foreach (var settingsCollection in AllContainedEditableSettingsCollections)
            {
                settingsCollection.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
            }
        }

        protected abstract void InitEditableSettingsAndCollections(T dataObject);

        protected abstract IEnumerable<IEditableSetting> EnumerateEditableSettings();

        protected abstract IEnumerable<IEditableSettingsCollectionV2> EnumerateEditableSettingsCollections();
    }

    /// <summary>
    /// Base class for settings collections.
    /// </summary>
    /// <remarks>
    /// Unique collection logic belongs in this class or a child of it, not in
    /// concrete model collections. This class and any such parent requires unit
    /// tests; concrete model collections only need composition tests.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class EditableSettingsCollectionV2<T> : EditableSettingsCollectionBase<T>, IEditableSettingsCollectionSpecific<T>
    {
        public EditableSettingsCollectionV2(
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
        {
            AllContainedEditableSettings = editableSettings;
            AllContainedEditableSettingsCollections = editableSettingsCollections;

            foreach (var setting in AllContainedEditableSettings)
            {
                // TODO: revisit settings composition so that this null check is unnecessary
                if (setting != null)
                {
                    setting.PropertyChanged += EditableSettingChangedEventHandler;
                }
            }

            // TODO: separate "All" and "currently selected" settings collections
            // so that incorrect assignment is not done here for collections that alter this through use
            foreach (var settingsCollection in AllContainedEditableSettingsCollections)
            {
                settingsCollection.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
            }
        }

        public bool TryMapFromData(T data)
        {
            bool result = true;

            result &= TryMapEditableSettingsFromData(data);
            result &= TryMapEditableSettingsCollectionsFromData(data);

            return result;
        }

        protected abstract bool TryMapEditableSettingsFromData(T data);

        protected abstract bool TryMapEditableSettingsCollectionsFromData(T data);
    }

    public abstract class NamedEditableSettingsCollection<T> : EditableSettingsCollectionV2<T>, INamedEditableSettingsCollectionSpecific<T>
    {
        public NamedEditableSettingsCollection(
            IEditableSettingSpecific<string> name,
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings.Union([name]), editableSettingsCollections)
        {
            Name = name;
        }

        public IEditableSettingSpecific<string> Name { get; }
    }
}
