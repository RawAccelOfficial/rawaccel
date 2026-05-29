using System;
using System.Text.Json;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class SettingsReaderWriter : ReaderWriterBase<Settings>
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        protected override string FileType => "Settings";

        public override string Serialize(Settings settings)
        {
            return JsonSerializer.Serialize(settings, JsonOptions);
        }

        public override Settings Deserialize(string toRead)
        {
            // Literal "null" -> defaults; malformed JSON throws for the caller
            // (BackEndLoader.LoadSettings) to handle, matching sibling readers.
            return JsonSerializer.Deserialize<Settings>(toRead, JsonOptions) ?? new Settings();
        }
    }
}