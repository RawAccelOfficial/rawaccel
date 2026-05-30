using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RawAccel.Contracts
{
    // JSON names match wrapper.cpp so settings.json round-trips unchanged.
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccelMode
    {
        classic,
        jump,
        natural,
        synchronous,
        power,
        lut,
        noaccel,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CapMode
    {
        in_out,
        input,
        output,
    }
}
