namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class SynchronousAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Synchronous;

        public double SyncSpeed { get; set; }

        public double Motivity { get; set; }

        public double Gamma { get; set; }

        public double Smoothness { get; set; }
    }
}
