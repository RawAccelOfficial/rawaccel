namespace userspace_backend.Driver
{
    // Live input speed in counts/ms normalized to 1000 DPI (curve-math units).
    // Combined is the agent-computed magnitude (not necessarily hypot(X, Y)).
    public readonly record struct MouseSpeedSample(double X, double Y, double Combined)
    {
        public static readonly MouseSpeedSample Zero = new(0, 0, 0);

        public bool IsZero => X == 0 && Y == 0 && Combined == 0;
    }
}
