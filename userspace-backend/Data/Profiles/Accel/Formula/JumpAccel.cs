namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class JumpAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Jump;

        public double Smooth { get; set; } = FormulaDefaults.JumpSmooth;

        public double Input { get; set; } = FormulaDefaults.JumpInput;

        public double Output { get; set; } = FormulaDefaults.JumpOutput;
    }
}
