using RawAccel.Contracts;

namespace userspace_backend.Driver
{
    // Platform-agnostic apply/read/deactivate surface for the driver.
    //
    // Error contract (mirrored by both the Windows and Linux implementations):
    // - Apply returns false on failure (logged); it does not throw.
    // - Read and Deactivate throw on failure (they are expected to succeed once
    //   IsAvailable is true).
    // - GetCurrentMouseSpeedSample returns MouseSpeedSample.Zero on error; it
    //   never throws, so callers can't distinguish "idle" from "unavailable".
    public interface IRawAccelDriver
    {
        // UI gates Apply on this.
        bool IsAvailable { get; }

        bool Apply(RawAccelConfig config);

        RawAccelConfig Read();

        void Deactivate();

        MouseSpeedSample GetCurrentMouseSpeedSample();
    }
}
