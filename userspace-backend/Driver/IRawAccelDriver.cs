using RawAccel.Contracts;

namespace userspace_backend.Driver
{
    // Platform-agnostic driver surface.
    //
    // Error contract (Windows and Linux impls both):
    // - Apply: returns false on failure, logs, never throws.
    // - Read, Deactivate: throw on failure (expected to succeed once IsAvailable).
    // - GetCurrentMouseSpeedSample: Zero on error, never throws -- callers
    //   can't distinguish idle from unavailable.
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
