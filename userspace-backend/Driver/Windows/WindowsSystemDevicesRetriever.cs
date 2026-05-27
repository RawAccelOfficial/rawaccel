using System.Collections.Generic;
using System.Linq;
using userspace_backend.Model;

namespace userspace_backend.Driver.Windows
{
    // Windows device enumeration. Uses the wrapper.dll's MultiHandleDevice.
    public sealed class WindowsSystemDevicesRetriever : ISystemDevicesRetriever
    {
        public IList<ISystemDevice> GetSystemDevices()
        {
            IList<MultiHandleDevice> rawDevices = MultiHandleDevice.GetList();
            return rawDevices.Select(d => new WindowsSystemDevice(d)).ToList<ISystemDevice>();
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
