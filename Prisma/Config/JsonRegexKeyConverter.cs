using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Prisma.Config;

internal sealed class JsonRegexKeyConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Dictionary<Regex,string>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => (JsonConverter)Activator.CreateInstance(
        typeof(DictionaryWithRegexKeyConverter),
        BindingFlags.Instance | BindingFlags.Public,
        null,
        new object[] { options },
        null
    )!;

    private sealed class DictionaryWithRegexKeyConverter : JsonConverter<Dictionary<Regex, string>>
    {
        // Constructor is mandatory.
        public DictionaryWithRegexKeyConverter(JsonSerializerOptions _) {}

        public override Dictionary<Regex, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected JsonTokenType.StartObject, got {reader.TokenType}");
            }

            Dictionary<Regex, string> dictionary = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected JsonTokenType.PropertyName, got {reader.TokenType}");
                }

                Regex key;

                try
                {
                    string? pattern = reader.GetString();

                    if (pattern == null)
                    {
                        throw new JsonException("Key is not a valid regular expression!");
                    }

                    // Give 2.5 seconds as maximum to prevent slow regexes from hanging.
                    key = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(2500));
                }
                catch (ArgumentException e)
                {
                    throw new JsonException("Key is not a valid regular expression!", e);
                }

                // Advance to the next token.
                if (!reader.Read())
                {
                    throw new JsonException("Value is not a valid string!");
                }

                string? value = reader.GetString();

                if (value == null)
                {
                    throw new JsonException("Value is not a valid string!");
                }

                dictionary.Add(key, value);
            }

            throw new JsonException("Unexpected end of JSON!");
        }


        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode", Justification = "Referenced type is an integral part of .NET")]
        public override void Write(Utf8JsonWriter writer, Dictionary<Regex, string> dictionary, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach ((Regex key, string value) in dictionary)
            {
                writer.WritePropertyName(key.ToString());
                JsonSerializer.Serialize(writer, value, options);
            }

            writer.WriteEndObject();
        }
    }
}
