using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace userspace_backend.Data
{
    public class Mapping
    {
        public static readonly MappingEqualityComparer EqualityComparer = new MappingEqualityComparer();

        [JsonRequired]
        public string Name { get; set; }

        [JsonRequired]
        public GroupsToProfilesMapping GroupsToProfiles { get; set; }

        public override bool Equals(object? obj)
        {
            bool isEqual = obj is Mapping mapping
                && string.Equals(Name, mapping.Name, StringComparison.InvariantCultureIgnoreCase)
                && mapping.GroupsToProfiles.Equals(this.GroupsToProfiles);

            return isEqual;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.ToUpperInvariant(), GroupsToProfiles);
        }

        public class GroupsToProfilesMapping : Dictionary<string, string>
        {
            public override bool Equals(object? obj)
            {
                return obj is GroupsToProfilesMapping mapping &&
                       Count == mapping.Count &&
                       this.All(kvp => 
                           mapping.TryGetValue(kvp.Key, out string mappingValue)
                           && string.Equals(mappingValue, kvp.Value, StringComparison.InvariantCultureIgnoreCase));
            }

            public override int GetHashCode()
            {
                HashCode hash = new HashCode();

                foreach (var kvp in this)
                {
                    hash.Add(kvp.GetHashCode());
                }

                return hash.ToHashCode();
            }
        }
    }

    public class MappingEqualityComparer : IEqualityComparer<Mapping>
    {
        public bool Equals(Mapping? x, Mapping? y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] Mapping obj)
        {
            return obj.GetHashCode();
        }
    }

    public class MappingSet
    {
        [JsonRequired]
        public Mapping[] Mappings { get; set; } = null!;

        public int ActiveMappingIndex { get; set; } = 0;

        public override bool Equals(object? obj)
        {
            MappingSet test = obj as MappingSet;
            return obj is MappingSet set
            && set.Mappings.Length == this.Mappings.Length
            && set.ActiveMappingIndex == this.ActiveMappingIndex
            && !set.Mappings.Except(this.Mappings, Mapping.EqualityComparer).Any();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mappings, ActiveMappingIndex);
        }
    }
}
