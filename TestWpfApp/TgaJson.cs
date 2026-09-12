using System.Text.Json;
using System.Text.Json.Serialization;
using TargaSharp;

namespace TestWpfApp
{
    /// <summary>
    /// Serializes a <see cref="TgaFile"/> to indented JSON for display. Large data arrays are summarized as their length.
    /// </summary>
    internal static class TgaJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new ArrayLengthConverter<byte>(),
                new ArrayLengthConverter<ushort>(),
                new ArrayLengthConverter<uint>(),
                new TgaStringConverter(),
            },
        };

        /// <summary>
        /// Serializes the given file to indented JSON.
        /// </summary>
        /// <param name="tga">File to serialize.</param>
        /// <returns>JSON text.</returns>
        public static string Serialize(TgaFile tga) => JsonSerializer.Serialize(tga, Options);

        /// <summary>
        /// Writes arrays as a "T[length]" summary rather than dumping their contents.
        /// </summary>
        /// <typeparam name="T">Array element type.</typeparam>
        private sealed class ArrayLengthConverter<T> : JsonConverter<T[]>
        {
            /// <inheritdoc />
            public override T[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options) => writer.WriteStringValue($"{typeof(T).Name}[{value.Length}]");
        }

        /// <summary>
        /// Writes a <see cref="TgaString"/> as its text value.
        /// </summary>
        private sealed class TgaStringConverter : JsonConverter<TgaString>
        {
            /// <inheritdoc />
            public override TgaString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, TgaString value, JsonSerializerOptions options) => writer.WriteStringValue(value.OriginalString);
        }
    }
}
