using Newtonsoft.Json;
using System;

namespace QIQI.EplOnCpp.Core
{
    public class CppTypeNameConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            var data = (CppTypeName[])value;
            writer.WriteValue(data.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            CppTypeName data;
            if (reader.TokenType == JsonToken.String)
            {
                string encodedData = reader.Value.ToString();
                data = CppTypeName.Parse(encodedData);
            }
            else
            {
                throw new Exception();
            }
            return data;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(CppTypeName));
        }
    }
}