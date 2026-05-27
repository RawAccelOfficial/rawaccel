using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class DevicesReaderWriter : ReaderWriterBase<IEnumerable<Device>>
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        protected override string FileType => "Devices";

        public override string Serialize(IEnumerable<Device> devices)
        {
            return JsonSerializer.Serialize(devices, JsonOptions);
        }

        public override IEnumerable<Device>? Deserialize(string toRead)
        {
            return JsonSerializer.Deserialize<List<Device>>(toRead, JsonOptions);
        }
    }
}
