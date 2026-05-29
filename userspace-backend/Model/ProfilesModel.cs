using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public interface IProfilesModel : IEditableSettingsList<IProfileModel, DATA.Profile>
    {
        ReadOnlyObservableCollection<IProfileModel> Profiles { get; }

        IProfileModel? DefaultProfile { get; }

        bool TryGetProfile(string name, out IProfileModel? profile);

        bool TryAddNewDefaultProfile(string name);

        bool RemoveProfile(IProfileModel profile);
    }

    public class ProfilesModel : EditableSettingsList<IProfileModel, DATA.Profile>, IProfilesModel
    {
        // Default profile is created by BackEnd.Load() if absent.

        public ProfilesModel(IServiceProvider serviceProvider)
            : base(serviceProvider, [], [])
        {
        }

        public ReadOnlyObservableCollection<IProfileModel> Profiles => Elements;

        public IProfileModel? DefaultProfile =>
            Elements.FirstOrDefault(p => string.Equals(p.Name.ModelValue, "default", StringComparison.InvariantCultureIgnoreCase));

        public bool TryGetProfile(string name, out IProfileModel? profile) => TryGetElement(name, out profile);

        public bool TryAddNewDefaultProfile(string name)
        {
            if (ContainsElementWithName(name))
            {
                return false;
            }

            IProfileModel newProfile = GenerateDefaultElement(name);
            AddElement(newProfile);
            return true;
        }

        public bool RemoveProfile(IProfileModel profile) => TryRemoveElement(profile);

        protected override string DefaultNameTemplate => "Profile";

        protected override string GetNameFromElement(IProfileModel element)
        {
            return element.Name.ModelValue;
        }

        protected override bool TryMapEditableSettingsFromData(IEnumerable<DATA.Profile> data)
        {
            return true;
        }

        protected override void SetElementName(IProfileModel element, string name)
        {
            element.Name.TryUpdateModelDirectly(name);
        }

        protected override string GetNameFromData(DATA.Profile data)
        {
            return data.Name;
        }
    }

    public class ProfileNameValidator(IProfilesModel profiles) : IModelValueValidator<string>
    {
        protected IProfilesModel Profiles { get; } = profiles;

        public bool Validate(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value.Length <= MaxNameLengthValidator.MaxNameLength
                && !Profiles.TryGetProfile(value, out _);
        }
    }
}
