namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class PowerAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Power;

        public double Scale { get; set; } = FormulaDefaults.PowerScale;

        public double Exponent { get; set; } = FormulaDefaults.PowerExponent;

        public double OutputOffset { get; set; } = FormulaDefaults.PowerOutputOffset;

        public double Cap { get; set; } = FormulaDefaults.PowerCap;
    }
}
