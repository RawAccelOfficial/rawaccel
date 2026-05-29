using System;
using Newtonsoft.Json;
using RawAccel.Contracts;

namespace userspace_backend.Driver.Windows
{
    // IAccelEvaluator backed by wrapper.ManagedAccel against the same
    // common/ math the kernel driver runs. Converts the RawAccelProfile
    // POCO into a wrapper.Profile via JSON round-trip (matching JsonProperty
    // names on both sides) so this evaluator stays in lockstep with whatever
    // the apply path produces.
    public sealed class ManagedAccelEvaluator : IAccelEvaluator
    {
        public IAccelInstance CreateInstance(RawAccelProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var json = JsonConvert.SerializeObject(profile);
            var nativeProfile = JsonConvert.DeserializeObject<Profile>(json)
                ?? throw new InvalidOperationException(
                    "POCO -> wrapper.Profile deserialization returned null");
            // Dispose the seed; CreateStatelessCopy allocates a fresh native pair.
            using var seed = new ManagedAccel(nativeProfile);
            var accel = seed.CreateStatelessCopy();
            return new ManagedAccelInstance(accel);
        }

        private sealed class ManagedAccelInstance : IAccelInstance
        {
            private readonly ManagedAccel accel;

            public ManagedAccelInstance(ManagedAccel accel)
            {
                this.accel = accel;
            }

            public (double x, double y) Accelerate(
                double x, double y, double dpiFactor, double timeMs)
            {
                var t = accel.Accelerate(x, y, dpiFactor, timeMs);
                return (t.Item1, t.Item2);
            }

            public void Dispose() => accel.Dispose();
        }
    }
}
