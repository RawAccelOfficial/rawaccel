using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

/**
 * TODO: Fix circular dependency and initialization order issues with ProfileNameValidator
 * 
 * - ProfilesModel needs ProfileNameValidator to create ProfileModel instances
 * - ProfileNameValidator needs ProfilesModel to check for duplicate names
 * 
 *  - Base constructor calls InitEditableSettingsAndCollections() BEFORE derived constructor can set NameValidator property, causing validator to be null
 *  
 *  - SOLUTION (Implement after DI PR from _m00se):
 *  
 *  Create IProfileNameChecker interface for duplicate name validation
 *  Have ProfilesModel implement IProfileNameChecker
 *  Inject IProfileNameChecker into ProfileNameValidator constructor
 *  Register ProfileNameValidator in DI container
 *  Inject ProfileNameValidator into ProfilesModel constructor
 */

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
        // Default profile is created during BackEnd.Load() if it doesn't exist

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
}
