using System;
using System.Text.Json;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class SettingsReaderWriter : ReaderWriterBase<Settings>
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected override string FileType => "Settings";

        public override string Serialize(Settings settings)
        {
            return JsonSerializer.Serialize(settings, JsonOptions);
        }

        public override Settings Deserialize(string toRead)
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(toRead, JsonOptions) ?? new Settings();
            }
            catch (JsonException)
            {
                return new Settings();
            }
        }
    }
}