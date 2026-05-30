using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using userspace_backend.Model;

namespace userspace_backend_tests.ModelTests
{
    // Cross-platform tests for the SystemDevices abstraction. The Windows-only
    // RawInput retriever is covered by WindowsSystemDevicesTests.
    [TestClass]
    public class SystemDevicesTests
    {
        private class TestDevicesRetriever : ISystemDevicesRetriever
        {
            public List<ISystemDevice> Devices { get; set; } = new List<ISystemDevice>();

            public IList<ISystemDevice> GetSystemDevices() => Devices;
        }

        private class TestSystemDevice : ISystemDevice
        {
            public string Name { get; set; } = string.Empty;

            public string HWID { get; set; } = string.Empty;
        }

        [TestMethod]
        public void SystemDevicesProvider_ProvidesDevices()
        {
            List<ISystemDevice> testSystemDevices = new List<ISystemDevice>()
            {
                new TestSystemDevice()
                {
                    Name = "test",
                    HWID = "garble",
                }
            };

            TestDevicesRetriever testDevicesRetriever = new TestDevicesRetriever()
            {
                Devices = testSystemDevices,
            };
            var services = new ServiceCollection();
            services.Add(ServiceDescriptor.Describe(
                serviceType: typeof(ISystemDevicesRetriever),
                implementationFactory: _ => testDevicesRetriever,
                lifetime: ServiceLifetime.Singleton));
            services.AddSingleton<SystemDevicesProvider>();
            var serviceProvider = services.BuildServiceProvider();

            SystemDevicesProvider testObject = serviceProvider.GetRequiredService<SystemDevicesProvider>();

            Assert.IsNotNull(testObject);
            Assert.AreEqual(testSystemDevices.Count, testObject.SystemDevices.Count);
            CollectionAssert.AreEquivalent(testSystemDevices, testObject.SystemDevices);
        }
    }
}
