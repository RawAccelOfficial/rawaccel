using System.Collections.Generic;
using System.Linq;
using userspace_backend.Model;

namespace userspace_backend.Driver.Windows
{
    // Windows-only system device enumeration. Uses the wrapper.dll's
    // MultiHandleDevice (RawInput-based) which is why this file lives under
    // Driver/Windows/ and is excluded from non-Windows builds via the
    // csproj Compile Remove rule.
    public sealed class WindowsSystemDevicesRetriever : ISystemDevicesRetriever
    {
        public IList<ISystemDevice> GetSystemDevices()
        {
            IList<MultiHandleDevice> rawDevices = MultiHandleDevice.GetList();
            return rawDevices.Select(d => new WindowsSystemDevice(d) as ISystemDevice).ToList();
        }
    }

    public sealed class WindowsSystemDevice : ISystemDevice
    {
        public WindowsSystemDevice(MultiHandleDevice multiHandleDevice)
        {
            RawDevice = multiHandleDevice;
        }

        public string Name => RawDevice.name;

        public string HWID => RawDevice.id;

        private MultiHandleDevice RawDevice { get; }
    }
}
