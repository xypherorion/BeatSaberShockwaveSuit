using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShockwaveSuit {
    public class QuaternionConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var c = (Quaternion)value;
            var objValue = new { c.x, c.y, c.z, c.w };
            serializer.Serialize(writer, objValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                return new Quaternion();
            } else {
                JObject obj = JObject.Load(reader);
                return new Quaternion(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"), obj.Value<float>("w"));
            }
        }

        public override bool CanConvert(Type objectType) {
            return (objectType == typeof(Quaternion));
        }
    }

}
