namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class NaturalAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Natural;

        public double DecayRate { get; set; } = 0.1;

        public double InputOffset { get; set; }

        public double Limit { get; set; } = 1.5;
    }
}
