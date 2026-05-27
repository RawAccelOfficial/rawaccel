using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model
{
    /// <summary>
    /// Holds system devices in observable collection and refreshes list when desired.
    /// </summary>
    public interface ISystemDevicesProvider
    {
        ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        void RefreshSystemDevices();
    }

    /// <summary>
    /// Application implementation of <see cref="ISystemDevicesProvider"/>
    /// </summary>
    public class SystemDevicesProvider : ISystemDevicesProvider
    {
        public SystemDevicesProvider(ISystemDevicesRetriever devicesRetriever)
        {
            DevicesRetriever = devicesRetriever;
            SystemDevicesInternal = new ObservableCollection<ISystemDevice>();
            SystemDevices = new ReadOnlyObservableCollection<ISystemDevice>(SystemDevicesInternal);
            RefreshSystemDevices();
        }

        public ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        protected ObservableCollection<ISystemDevice> SystemDevicesInternal { get; }

        protected ISystemDevicesRetriever DevicesRetriever { get; }

        public void RefreshSystemDevices()
        {
            // TODO: Replace with "addrange" equivalent from ObservableCollection child class
            SystemDevicesInternal.Clear();
            IList<ISystemDevice> retrievedDevices = DevicesRetriever.GetSystemDevices();

            foreach (ISystemDevice retrievedDevice in retrievedDevices)
            {
                SystemDevicesInternal.Add(retrievedDevice);
            }
        }
    }

    /// <summary>
    /// Retrieves list of devices from operating system. Concrete impls are
    /// per-platform: WindowsSystemDevicesRetriever (RawInput via wrapper.dll)
    /// or LinuxSystemDevicesRetriever (currently a stub; future: query the
    /// agent or read /dev/input directly).
    /// </summary>
    public interface ISystemDevicesRetriever
    {
        IList<ISystemDevice> GetSystemDevices();
    }

    /// <summary>
    /// Interface to represent devices as they come from the operating system.
    /// Backing impls live next to their per-platform retrievers.
    /// </summary>
    public interface ISystemDevice
    {
        public string Name { get; }

        public string HWID { get; }
    }
}
