using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RawAccel.Contracts
{
    // JSON values: classic, jump, natural, synchronous, power, lut, noaccel.
    // Names match wrapper/wrapper.cpp exactly so existing settings.json files
    // round-trip unchanged.
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

    // JSON values: in_out, input, output. Same shape as wrapper/wrapper.cpp.
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CapMode
    {
        in_out,
        input,
        output,
    }
}
