using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class MappingsReaderWriter : ReaderWriterBase<MappingSet>
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        protected override string FileType => "Mappings";

        public override string Serialize(MappingSet toWrite)
        {
            return JsonSerializer.Serialize(toWrite, JsonOptions);
        }

        public override MappingSet? Deserialize(string toRead)
        {
            return JsonSerializer.Deserialize<MappingSet>(toRead, JsonOptions);
        }
    }
}
