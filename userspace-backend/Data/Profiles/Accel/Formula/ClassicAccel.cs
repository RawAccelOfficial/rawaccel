namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class ClassicAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Classic;

        public double Acceleration { get; set; }

        public double Exponent { get; set; }

        public double Offset { get; set; }

        public double Cap { get; set; }
    }
}
