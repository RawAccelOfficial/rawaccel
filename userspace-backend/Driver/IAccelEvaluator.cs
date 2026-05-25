using RawAccel.Contracts;

namespace userspace_backend.Driver
{
    // Stateless per-sample acceleration evaluator used by the live curve
    // preview in Display/CurvePreview.cs. Decoupled from IRawAccelDriver
    // because preview is a pure-math read that doesn't touch the backend.
    //
    // Two-step usage (matches how ManagedAccel is used today):
    //   var instance = evaluator.CreateInstance(profile);
    //   foreach (var point in points) {
    //       var (ox, oy) = instance.Accelerate(point.x, point.y, 1, 1);
    //   }
    //
    // The Windows implementation wraps wrapper.ManagedAccel.CreateStatelessCopy
    // against the same common/ math the driver uses.
    public interface IAccelEvaluator
    {
        IAccelInstance CreateInstance(RawAccelProfile profile);
    }

    public interface IAccelInstance
    {
        // dpiFactor is device DPI normalized against NORMALIZED_DPI (1000);
        // timeMs is the time slice attributed to this sample (1.0 in the
        // existing preview). Returns the post-acceleration (x, y) in the
        // same units as input.
        (double x, double y) Accelerate(double x, double y, double dpiFactor, double timeMs);
    }
}
