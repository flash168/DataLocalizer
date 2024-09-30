using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataLocalizer
{
    public class JsonToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 将整个JSON值作为字符串读取
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString(); // 普通的字符串
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return null; // JSON为null
            }
            else
            {
                // 处理对象和其他复杂结构
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.GetRawText(); // 将整个结构作为字符串返回
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // 简单地写入字符串
            writer.WriteStringValue(value);
        }
    }
}
