namespace userspace_backend.Data.Profiles
{
    public class Hidden
    {
        public double RotationDegrees { get; set; }

        public double AngleSnappingDegrees { get; set; }

        public double LeftRightRatio { get; set; } = 1.0;

        public double UpDownRatio { get; set; } = 1.0;

        public double SpeedCap { get; set; }

        public double OutputSmoothingHalfLife { get; set; }
    }
}
