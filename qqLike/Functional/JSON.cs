using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace qqLike.Functional;

public class ByteArrayToIntArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 从数字数组转回字节数组
        return JsonSerializer.Deserialize<int[]>(ref reader)
            .Select(x => (byte)x).ToArray();
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        // 将字节数组转为数字数组
        writer.WriteStartArray();
        foreach (byte b in value)
        {
            writer.WriteNumberValue(b);
        }

        writer.WriteEndArray();
    }
}

public class JSON
{
    public static T Parse<T>(string json)
    {
        JsonDocument document = JsonDocument.Parse(json);
        return document.Deserialize<T>(
            new JsonSerializerOptions { Converters = { new ByteArrayToIntArrayConverter() } });
    }

    public static String Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj,
            new JsonSerializerOptions { Converters = { new ByteArrayToIntArrayConverter() } });
    }
}