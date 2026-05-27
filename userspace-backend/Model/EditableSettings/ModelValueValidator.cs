using System;

namespace userspace_backend.Model.EditableSettings
{
    public interface IModelValueValidator<T>
    {
        bool Validate(T value);
    }

    /// <summary>
    /// Rejects values outside an optional [min, max] range. Either bound may be
    /// omitted (open on that side) and either bound may be inclusive or exclusive.
    /// Rejection keeps the last good value (the validator simply returns false).
    /// </summary>
    public class RangeValidator<T> : IModelValueValidator<T> where T : struct, IComparable<T>
    {
        private readonly T? min;
        private readonly T? max;
        private readonly bool minInclusive;
        private readonly bool maxInclusive;

        public RangeValidator(T? min = null, T? max = null, bool minInclusive = true, bool maxInclusive = true)
        {
            this.min = min;
            this.max = max;
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        public bool Validate(T value)
        {
            if (min.HasValue)
            {
                int cmp = value.CompareTo(min.Value);
                if (minInclusive ? cmp < 0 : cmp <= 0)
                {
                    return false;
                }
            }

            if (max.HasValue)
            {
                int cmp = value.CompareTo(max.Value);
                if (maxInclusive ? cmp > 0 : cmp >= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class DefaultModelValueValidator<T> : IModelValueValidator<T>
    {
        public const string AllChangeInvalidDIKey = nameof(AllChangeInvalidDIKey);

        public bool Validate(T value)
        {
            return true;
        }
    }

    public class AllChangeInvalidValueValidator<T> : IModelValueValidator<T>
    {
        public bool Validate(T value) => false;
    }
}
