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
            Name = multiHandleDevice.name ?? string.Empty;
            HWID = multiHandleDevice.id ?? string.Empty;
        }

        public string Name { get; }

        public string HWID { get; }
    }
}
