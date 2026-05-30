namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class SynchronousAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Synchronous;

        public double SyncSpeed { get; set; } = FormulaDefaults.SyncSpeed;

        public double Motivity { get; set; } = FormulaDefaults.Motivity;

        public double Gamma { get; set; } = FormulaDefaults.Gamma;

        public double Smoothness { get; set; } = FormulaDefaults.Smoothness;
    }
}
