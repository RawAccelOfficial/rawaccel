namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class LinearAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Linear;

        public double Acceleration { get; set; } = FormulaDefaults.LinearAcceleration;

        public double Offset { get; set; } = FormulaDefaults.LinearOffset;

        public double Cap { get; set; } = FormulaDefaults.LinearCap;
    }
}
