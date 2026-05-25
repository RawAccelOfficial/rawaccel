using RawAccel.Contracts;

namespace userspace_backend.Driver
{
    // Platform-agnostic apply/read/deactivate surface for the Raw Accel
    // backend. The Windows implementation talks to the kernel filter driver
    // via IOCTL. Implementations consume and produce the same RawAccelConfig
    // POCO; the JSON contract for that POCO is the source of truth in
    // RawAccel.Contracts.
    public interface IRawAccelDriver
    {
        // True if this implementation can talk to its backend right now.
        // Windows: driver service installed and reachable. UI gates Apply
        // on this.
        bool IsAvailable { get; }

        // Push a configuration. Returns true on success, false if the
        // backend rejected the config or the transport failed. Implementations
        // should log the underlying error rather than letting it surface as
        // an exception so callers can render a simple success/fail toast.
        // The 1s WriteDelay anti-abuse mitigation is enforced by the backend
        // (driver / agent), not by this method.
        bool Apply(RawAccelConfig config);

        // Read the currently active configuration from the backend.
        RawAccelConfig Read();

        // Reset the backend to a no-op configuration without uninstalling.
        void Deactivate();

        // Optional telemetry: current input speed (counts/ms or in/s,
        // implementation-defined). Returns 0 when unsupported.
        double GetCurrentMouseSpeed();
    }
}
