namespace userspace_backend.Driver
{
    // Live input speed reported by the driver/agent, in counts/ms normalized to
    // 1000 DPI (the same units the curve math consumes). X and Y are per-axis;
    // Combined is the magnitude the agent computed (not necessarily hypot(X, Y)).
    public readonly record struct MouseSpeedSample(double X, double Y, double Combined)
    {
        public static readonly MouseSpeedSample Zero = new(0, 0, 0);

        public bool IsZero => X == 0 && Y == 0 && Combined == 0;
    }
}
