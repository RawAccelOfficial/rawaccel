using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RawAccel.Contracts;
using userspace_backend.Display;
using userspace_backend.Driver;

namespace userspace_backend_tests.DisplayTests
{
    // Regression tests for the IAccelInstance disposal contract.
    //
    // The bug: IAccelInstance had no IDisposable, so CurvePreview.GeneratePoints
    // created a fresh instance on every refresh and could not free it. On Linux
    // that instance (ShimInstance) holds a native ra_curve handle, so the preview
    // leaked one handle per refresh until finalization. These tests lock in that
    // GeneratePoints disposes what it creates and that the contract stays on the
    // interface, with no native shim or GUI required.
    [TestClass]
    public class CurvePreviewDisposalTests
    {
        private sealed class FakeInstance : IAccelInstance
        {
            public int AccelerateCount { get; private set; }
            public int DisposeCount { get; private set; }
            public bool ThrowOnAccelerate { get; set; }

            public (double x, double y) Accelerate(
                double x, double y, double dpiFactor, double timeMs)
            {
                AccelerateCount++;
                if (ThrowOnAccelerate) throw new InvalidOperationException("boom");
                return (x, y);
            }

            public void Dispose() => DisposeCount++;
        }

        private sealed class FakeEvaluator : IAccelEvaluator
        {
            public List<FakeInstance> Created { get; } = new();
            public bool NextThrowsOnAccelerate { get; set; }

            public IAccelInstance CreateInstance(RawAccelProfile profile)
            {
                var instance = new FakeInstance { ThrowOnAccelerate = NextThrowsOnAccelerate };
                Created.Add(instance);
                return instance;
            }
        }

        [TestMethod]
        public void IAccelInstance_ImplementsIDisposable()
        {
            // Guards the contract: removing ": IDisposable" from the interface
            // (which reintroduces the leak) fails here instead of silently.
            Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(IAccelInstance)));
        }

        [TestMethod]
        public void GeneratePoints_DisposesInstanceItCreated()
        {
            var evaluator = new FakeEvaluator();
            var preview = new CurvePreview(evaluator);

            preview.GeneratePoints(new RawAccelProfile());

            Assert.AreEqual(1, evaluator.Created.Count, "expected one instance per call");
            FakeInstance instance = evaluator.Created[0];
            Assert.IsTrue(instance.AccelerateCount > 0, "instance should have been used");
            Assert.AreEqual(1, instance.DisposeCount, "instance must be disposed exactly once");
        }

        [TestMethod]
        public void GeneratePoints_DisposesOneInstancePerCall()
        {
            var evaluator = new FakeEvaluator();
            var preview = new CurvePreview(evaluator);

            const int refreshes = 5;
            for (int i = 0; i < refreshes; i++)
            {
                preview.GeneratePoints(new RawAccelProfile());
            }

            Assert.AreEqual(refreshes, evaluator.Created.Count);
            foreach (FakeInstance instance in evaluator.Created)
            {
                Assert.AreEqual(1, instance.DisposeCount,
                    "every refresh's instance must be disposed exactly once");
            }
        }

        [TestMethod]
        public void GeneratePoints_DisposesInstance_WhenAccelerateThrows()
        {
            // Proves the call site uses using/try-finally, not just a trailing
            // Dispose() that an exception would skip past.
            var evaluator = new FakeEvaluator { NextThrowsOnAccelerate = true };
            var preview = new CurvePreview(evaluator);
            preview.SetPoints(new[] { new CurvePoint { MouseSpeed = 5.0 } });

            Assert.ThrowsException<InvalidOperationException>(
                () => preview.GeneratePoints(new RawAccelProfile()));

            Assert.AreEqual(1, evaluator.Created.Count);
            Assert.AreEqual(1, evaluator.Created[0].DisposeCount,
                "instance must be disposed even when evaluation throws");
        }
    }
}
