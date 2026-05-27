namespace userspace_backend.Data.Profiles
{
    public abstract class Acceleration
    {
        public enum AccelerationDefinitionType
        {
            None,
            Formula,
            LookupTable,
        }

        public abstract AccelerationDefinitionType Type { get; }

        public Anisotropy Anisotropy { get; set; } = new Anisotropy();

        public Coalescion Coalescion { get; set; } = new Coalescion();
    }
}
