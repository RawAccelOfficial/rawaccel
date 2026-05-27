using System;
using RawAccel.Contracts;

namespace userspace_backend.Driver
{
    // Decoupled from IRawAccelDriver because preview is a pure-math
    // read that doesn't touch the backend.
    //
    // Windows: wraps wrapper.ManagedAccel.CreateStatelessCopy against the
    // same common/ math the driver uses.
    // Linux: P/Invokes a stripped libcommon.so (deferred); identity stub
    // initially so the chart still renders.
    public interface IAccelEvaluator
    {
        IAccelInstance CreateInstance(RawAccelProfile profile);
    }

    // IDisposable because a native-backed instance (Linux ShimInstance) holds an
    // unmanaged ra_curve handle; preview callers create one per refresh.
    public interface IAccelInstance : IDisposable
    {
        // dpiFactor is device DPI normalized against NORMALIZED_DPI (1000);
        // timeMs is the time slice attributed to this sample (1 currently)
        // Returns same units as input.
        (double x, double y) Accelerate(double x, double y, double dpiFactor, double timeMs);
    }
}
