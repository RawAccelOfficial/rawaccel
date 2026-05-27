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
            // A literal "null" payload maps to defaults; malformed JSON is left to
            // throw so the caller (BackEndLoader.LoadSettings) can decide, the same
            // way the sibling reader/writers behave.
            return JsonSerializer.Deserialize<Settings>(toRead, JsonOptions) ?? new Settings();
        }
    }
}