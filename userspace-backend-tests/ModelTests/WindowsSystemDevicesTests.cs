using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using userspace_backend.Driver.Windows;
using userspace_backend.Model;

namespace userspace_backend_tests.ModelTests
{
    // Windows-only retriever test. Lives in a separate file from
    // SystemDevicesTests so the cross-platform provider test can run on Linux.
    // Excluded from non-Windows builds via the same csproj Compile Remove rule
    // that hides BackEndApplyTests.cs (kept Windows-only originally).
    // Asserts the Windows RawInput-based retriever returns at least one mouse;
    // skip on a headless build server where no mouse is connected.
    [TestClass]
    public class WindowsSystemDevicesTests
    {
        [TestMethod]
        public void WindowsSystemDevicesRetriever_RetrievesDevices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<WindowsSystemDevicesRetriever>();
            var serviceProvider = services.BuildServiceProvider();

            var testObject = serviceProvider.GetRequiredService<WindowsSystemDevicesRetriever>();
            Assert.IsNotNull(testObject);

            // These devices will be different per user, but should not be null
            // as long as the user has something giving mouse input.
            IList<ISystemDevice> retrievedDevices = testObject.GetSystemDevices();
            Assert.IsNotNull(retrievedDevices);
            Assert.IsTrue(retrievedDevices.Count > 0);
        }
    }
}
