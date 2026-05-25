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
            var json = JsonConvert.SerializeObject(profile);
            var nativeProfile = JsonConvert.DeserializeObject<Profile>(json)
                ?? throw new InvalidOperationException(
                    "POCO -> wrapper.Profile deserialization returned null");
            var accel = new ManagedAccel(nativeProfile).CreateStatelessCopy();
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
        }
    }
}
