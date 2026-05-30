using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSettingsList<T, U>
        : IEditableSettingsCollectionSpecific<IEnumerable<U>> where T : IEditableSettingsCollectionSpecific<U>
    {
        public ReadOnlyObservableCollection<T> Elements { get; }

        public bool TryAddNewDefault();

        public bool TryAdd(T element);

        public bool TryInsert(int index, T element);

        public bool TryGetElement(string name, out T? element);

        public bool TryRemoveElement(T element);
    }

    public abstract class EditableSettingsList<T, U>
        : EditableSettingsCollectionV2<IEnumerable<U>>, IEditableSettingsList<T, U> where T : IEditableSettingsCollectionSpecific<U>
    {
        public EditableSettingsList(
            IServiceProvider serviceProvider,
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings, editableSettingsCollections)
        {
            ServiceProvider = serviceProvider;
            ElementsInternal = new ObservableCollection<T>();
            Elements = new ReadOnlyObservableCollection<T>(ElementsInternal);
        }

        public ReadOnlyObservableCollection<T> Elements { get; }

        protected ObservableCollection<T> ElementsInternal { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected abstract string DefaultNameTemplate { get; }

        public override IEnumerable<U> MapToData()
        {
            return ElementsInternal.Select(e => e.MapToData());
        }

        public bool TryAdd(T element)
        {
            string name = GetNameFromElement(element);

            if (!ContainsElementWithName(name))
            {
                AddElement(element);
                return true;
            }

            return false;
        }

        public bool TryInsert(int index, T element)
        {
            string name = GetNameFromElement(element);

            if (!ContainsElementWithName(name))
            {
                element.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
                ElementsInternal.Insert(index, element);
                return true;
            }

            return false;
        }

        public bool TryAddNewDefault()
        {
            for (int i = 1; i < 10; i++)
            {
                string defaultProfileName = GenerateDefaultName(i);
                
                if (!ContainsElementWithName(defaultProfileName))
                {
                    T newDefaultElement = GenerateDefaultElement(defaultProfileName);
                    AddElement(newDefaultElement);

                    return true;
                }
            }

            return false;
        }

        public bool TryGetElement(string name, out T? element)
        {
            element = default;

            foreach (T elementInList in ElementsInternal)
            {
                if (string.Equals(name, GetNameFromElement(elementInList), StringComparison.InvariantCultureIgnoreCase))
                {
                    element = elementInList;
                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveElement(T element)
        {
            bool removed = ElementsInternal.Remove(element);

            if (removed)
            {
                element.AnySettingChanged -= EditableSettingsCollectionChangedEventHandler;
            }

            return removed;
        }

        protected bool ContainsElementWithName(string name) => TryGetElement(name, out T? _);

        protected string GenerateDefaultName(int index) => $"{DefaultNameTemplate}{index}";

        protected void AddElement(T element)
        {
            element.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
            ElementsInternal.Add(element);
        }

        protected T GenerateDefaultElement(string name)
        {
            T newDefaultElement = ServiceProvider.GetRequiredService<T>();
            SetElementName(newDefaultElement, name);

            return newDefaultElement;
        }

        protected abstract string GetNameFromElement(T element);

        protected abstract void SetElementName(T element, string name);

        protected abstract string GetNameFromData(U data);

        protected override bool TryMapEditableSettingsCollectionsFromData(IEnumerable<U> data)
        {
            bool result = true;

            var incomingNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (U dataElement in data)
            {
                string elementName = GetNameFromData(dataElement);
                incomingNames.Add(elementName);

                if (!TryGetElement(elementName, out T? element))
                {
                    element = GenerateDefaultElement(elementName);

                    AddElement(element);
                }

                result &= element!.TryMapFromData(dataElement);
            }

            // The incoming data is the source of truth: drop elements it no longer contains.
            // OS-detected devices are merged separately (ImportSystemDevices), not through here.
            foreach (T stale in ElementsInternal
                .Where(e => !incomingNames.Contains(GetNameFromElement(e)))
                .ToList())
            {
                TryRemoveElement(stale);
            }

            return result;
        }
    }
}
