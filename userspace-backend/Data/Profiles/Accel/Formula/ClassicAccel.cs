namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class ClassicAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Classic;

        public double Acceleration { get; set; } = FormulaDefaults.ClassicAcceleration;

        public double Exponent { get; set; } = FormulaDefaults.ClassicExponent;

        public double Offset { get; set; } = FormulaDefaults.ClassicOffset;

        public double Cap { get; set; } = FormulaDefaults.ClassicCap;
    }
}
