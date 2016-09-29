// <copyright company="SIX Networks GmbH" file="UnixTimestampConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Newtonsoft.Json;
using withSIX.Steam.Core.Extensions;

namespace withSIX.Steam.Api.SteamKit.Utils
{
    public sealed class UnixTimestampConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value != null)
                writer.WriteValue(((DateTime) value).ToUnixTimestamp());
            else
                writer.WriteUndefined();
            //throw new NotImplementedException();
            //writer.WriteValue();
            //JToken t = JToken.FromObject(value);
            //JObject o = (JObject)t;
            //o.
            //IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();
            //o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));
            //o.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            if (reader.Value != null)
                return reader.Value.ToString().UnixTimeToDateTime();
            return null;
        }

        public override bool CanConvert(Type objectType) => true;
    }
}