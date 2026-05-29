using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using static userspace_backend.Data.Profiles.Accel.FormulaAccel;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend.IO.Serialization
{
    public class AccelerationJsonConverter : JsonConverter<Acceleration>
    {
        public override Acceleration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader typeReader = reader;
            int startDepth = reader.CurrentDepth;

            string typeString = ReadToType(ref reader);
            ReadToEnd(ref reader, startDepth);

            string[] typeStringSplit = typeString.Split('/');
            string baseType = typeStringSplit[0];
            AccelerationDefinitionType definitionType = DetermineDefinitionType(baseType);

            return CreateAccelerationOfType(definitionType, typeStringSplit, ref typeReader);
        }

        private static string ReadToType(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName
                    && reader.GetString() == "Type")
                {
                    break;
                }
            }

            if (!reader.Read())
            {
                throw new JsonException("Acceleration definition must include \"Type\".");
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Type must be a string.");
            }

            string typeString = reader.GetString();

            if (string.IsNullOrEmpty(typeString))
            {
                throw new JsonException("\"Type\" must have a non-empty and non-null value.");
            }

            return typeString;
        }

        private static void ReadToEnd(ref Utf8JsonReader reader, int depth)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == depth)
                {
                    return;
                }
            }
        }

        private static AccelerationDefinitionType DetermineDefinitionType(string typeStringFromJson)
        {
            if (!Enum.TryParse(typeStringFromJson, ignoreCase: true, out AccelerationDefinitionType result))
            {
                throw new JsonException($"Acceleration base type [\"{typeStringFromJson}\"] not valid. " +
                    $"Valid values: [{string.Join(", ", Enum.GetNames(typeof(AccelerationDefinitionType)))}]");
            }

            return result;
        }

        private static Acceleration CreateAccelerationOfType(AccelerationDefinitionType defnType, string[] defnSplit, ref Utf8JsonReader readerFromStart)
        {
            switch (defnType)
            {
                case AccelerationDefinitionType.Formula:
                    return CreateFormulaAccel(defnSplit, ref readerFromStart);
                case AccelerationDefinitionType.LookupTable:
                    return CreateLookupTableAccel(ref readerFromStart);
                case AccelerationDefinitionType.None:
                default:
                    return new NoAcceleration();
            }
        }

        private static FormulaAccel CreateFormulaAccel(string[] defnSplit, ref Utf8JsonReader readerFromStart)
        {
            if (defnSplit.Length < 2)
            {
                throw new JsonException("Type \"Formula\" must be followed by a forward slash and formula type. Example: \"Type\": \"Formula/Classic\"");
            }

            string formulaTypeString = defnSplit[1];

            if (string.IsNullOrEmpty(formulaTypeString))
            {
                throw new JsonException("Formula/[Type] must have a non-empty and non-null value.");
            }

            AccelerationFormulaType formulaType = DetermineFormulaType(formulaTypeString);

            switch (formulaType)
            {
                case AccelerationFormulaType.Synchronous:
                    return JsonSerializer.Deserialize<SynchronousAccel>(ref readerFromStart);
                case AccelerationFormulaType.Linear:
                    return JsonSerializer.Deserialize<LinearAccel>(ref readerFromStart);
                case AccelerationFormulaType.Classic:
                    return JsonSerializer.Deserialize<ClassicAccel>(ref readerFromStart);
                case AccelerationFormulaType.Power:
                    return JsonSerializer.Deserialize<PowerAccel>(ref readerFromStart);
                case AccelerationFormulaType.Natural:
                    return JsonSerializer.Deserialize<NaturalAccel>(ref readerFromStart);
                case AccelerationFormulaType.Jump:
                    return JsonSerializer.Deserialize<JumpAccel>(ref readerFromStart);
                default:
                    throw new JsonException($"Unknown formula type {formulaTypeString}");
            }
        }

        private static AccelerationFormulaType DetermineFormulaType(string formulaTypeFromJson)
        {
            if (!Enum.TryParse(formulaTypeFromJson, ignoreCase: true, out AccelerationFormulaType result))
            {
                throw new JsonException($"Acceleration formula type [\"{formulaTypeFromJson}\"] not valid. " +
                    $"Valid values: [{string.Join(", ", Enum.GetNames(typeof(AccelerationFormulaType)))}]");
            }

            return result;
        }

        private static LookupTableAccel CreateLookupTableAccel(ref Utf8JsonReader readerFromStart)
        {
            return JsonSerializer.Deserialize<LookupTableAccel>(ref readerFromStart);
        }

        public override void Write(Utf8JsonWriter writer, Acceleration value, JsonSerializerOptions options)
        {
            // Serialize the runtime type's fields, then overwrite "Type" with
            // the discriminator Read expects (e.g. "Formula/Classic", "LookupTable",
            // "None"). The fresh options instance excludes this converter to
            // avoid recursing into ourselves.
            JsonNode? node = JsonSerializer.SerializeToNode(
                value, value.GetType(), WriteOptionsWithoutSelf(options));

            if (node is JsonObject obj)
            {
                obj["Type"] = GetDiscriminator(value);
                // FormulaType is folded into Type ("Formula/Classic"); don't emit twice.
                obj.Remove("FormulaType");
            }
            node?.WriteTo(writer);
        }

        private static string GetDiscriminator(Acceleration v) => v switch
        {
            NoAcceleration => "None",
            LookupTableAccel => "LookupTable",
            FormulaAccel f => $"Formula/{f.FormulaType}",
            _ => v.Type.ToString(),
        };

        // Cache derived options keyed by source identity. A reader/writer reuses
        // one JsonSerializerOptions for its lifetime, so this is a near-perfect hit;
        // a different source just re-derives. The first-call race wastes at most
        // one copy -- no locking needed.
        private JsonSerializerOptions? cachedSource;
        private JsonSerializerOptions? cachedWriteOptions;

        private JsonSerializerOptions WriteOptionsWithoutSelf(JsonSerializerOptions src)
        {
            if (!ReferenceEquals(src, cachedSource))
            {
                var copy = new JsonSerializerOptions(src);
                for (int i = copy.Converters.Count - 1; i >= 0; --i)
                {
                    if (copy.Converters[i] is AccelerationJsonConverter)
                        copy.Converters.RemoveAt(i);
                }
                cachedSource = src;
                cachedWriteOptions = copy;
            }

            return cachedWriteOptions!;
        }
    }
}
