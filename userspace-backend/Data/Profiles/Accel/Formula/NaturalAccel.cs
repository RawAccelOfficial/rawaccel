namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class NaturalAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Natural;

        public double DecayRate { get; set; } = FormulaDefaults.NaturalDecayRate;

        public double InputOffset { get; set; } = FormulaDefaults.NaturalInputOffset;

        public double Limit { get; set; } = FormulaDefaults.NaturalLimit;
    }
}
