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
            return obj is Mapping mapping
                && string.Equals(Name, mapping.Name, StringComparison.OrdinalIgnoreCase)
                && (GroupsToProfiles?.Equals(mapping.GroupsToProfiles) ?? mapping.GroupsToProfiles is null);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
                GroupsToProfiles?.GetHashCode() ?? 0);
        }

        public class GroupsToProfilesMapping : Dictionary<string, string>
        {
            public override bool Equals(object? obj)
            {
                return obj is GroupsToProfilesMapping mapping &&
                       Count == mapping.Count &&
                       this.All(kvp =>
                           mapping.TryGetValue(kvp.Key, out string? mappingValue)
                           && string.Equals(mappingValue, kvp.Value, StringComparison.OrdinalIgnoreCase));
            }

            public override int GetHashCode()
            {
                // XOR per-entry hashes so the result is order-independent,
                // matching the order-independent Equals above. Keys use the
                // dictionary's (ordinal) comparer; values are case-insensitive.
                int hash = 0;

                foreach (var kvp in this)
                {
                    int valueHash = kvp.Value is null
                        ? 0
                        : StringComparer.OrdinalIgnoreCase.GetHashCode(kvp.Value);
                    hash ^= HashCode.Combine(kvp.Key, valueHash);
                }

                return hash;
            }
        }
    }

    public class MappingEqualityComparer : IEqualityComparer<Mapping>
    {
        public bool Equals(Mapping? x, Mapping? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
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
            if (obj is not MappingSet set) return false;
            if (ActiveMappingIndex != set.ActiveMappingIndex) return false;
            if (ReferenceEquals(Mappings, set.Mappings)) return true;
            if (Mappings is null || set.Mappings is null) return false;

            return Mappings.Length == set.Mappings.Length
                && !set.Mappings.Except(Mappings, Mapping.EqualityComparer).Any();
        }

        public override int GetHashCode()
        {
            // XOR element hashes so the hash is order-independent, consistent
            // with the set-based Equals; combine with the positional index.
            int mappingsHash = 0;
            if (Mappings is not null)
            {
                foreach (Mapping mapping in Mappings)
                {
                    mappingsHash ^= mapping?.GetHashCode() ?? 0;
                }
            }

            return HashCode.Combine(mappingsHash, ActiveMappingIndex);
        }
    }
}
