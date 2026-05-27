namespace userspace_backend.Data.Profiles.Accel
{
    public abstract class FormulaAccel : Acceleration
    {
        public enum AccelerationFormulaType
        {
            Synchronous = 0,
            Linear = 1,
            Classic = 2,
            Power = 3,
            Natural = 4,
            Jump = 5,
        }

        public override AccelerationDefinitionType Type => AccelerationDefinitionType.Formula;

        public abstract AccelerationFormulaType FormulaType { get; }

        public bool Gain { get; set; }
    }
}
