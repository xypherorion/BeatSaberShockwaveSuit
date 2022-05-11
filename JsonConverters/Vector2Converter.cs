using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShockwaveSuit {
    public class Vector2Converter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var c = (Vector2)value;
            var objValue = new { c.x, c.y};
            serializer.Serialize(writer, objValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                return new Vector2();
            } else {
                JObject obj = JObject.Load(reader);
                return new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
            }
        }

        public override bool CanConvert(Type objectType) {
            return (objectType == typeof(Vector2));
        }
    }

}
