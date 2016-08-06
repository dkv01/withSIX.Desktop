// <copyright company="SIX Networks GmbH" file="SerializationExtension.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using NDepend.Path;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace SN.withSIX.Core.Extensions
{
    public static class SerializationExtension
    {
        public static readonly JsonSerializerSettings DefaultSettings =
            new JsonSerializerSettings().SetDefaultSettings();

        public static string ToJson(this object @this, bool pretty = false) => @this.ToJson(DefaultSettings, pretty);
        public static T FromJson<T>(this string @this) => @this.FromJson<T>(DefaultSettings);

        public static T FromJson<T>(this string @this, JsonSerializerSettings settings) {
            Contract.Requires<ArgumentNullException>(@this != null);
            Contract.Requires<ArgumentNullException>(@this != null);

            return JsonConvert.DeserializeObject<T>(@this, settings);
        }

        public static string ToJson(this object @this, JsonSerializerSettings settings, bool pretty = false) {
            Contract.Requires<ArgumentNullException>(@this != null);
            var json = pretty
                ? JsonConvert.SerializeObject(@this, Formatting.Indented, settings)
                : JsonConvert.SerializeObject(@this, settings);
            return json;
        }

        public static JsonSerializerSettings SetDefaultSettings(this JsonSerializerSettings settings) {
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.SetDefaultConverters();

            return settings;
        }

        public static JsonSerializerSettings SetDefaultConverters(this JsonSerializerSettings settings) {
            settings.Converters.Add(new AbsoluteDirectoryPathConverter());
            settings.Converters.Add(new AbsoluteFilePathConverter());
            settings.Converters.Add(new IPAddressConverter());
            settings.Converters.Add(new IPEndPointConverter());
            settings.Converters.Add(new VersionConverter());
            settings.ContractResolver = new CustomContractResolver();

            return settings;
        }
    }

    public class CustomContractResolver : DefaultContractResolver
    {
        static readonly Dictionary<Type, JsonConverter> converterMapping = new Dictionary<Type, JsonConverter> {
            {typeof (IAbsoluteDirectoryPath), new AbsoluteDirectoryPathConverter()},
            {typeof (IAbsoluteFilePath), new AbsoluteFilePathConverter()}
        };

        public override JsonContract ResolveContract(Type type)
            =>
                converterMapping.Keys.Any(t => t.IsAssignableFrom(type))
                    ? CreateStringContract(type)
                    : base.ResolveContract(type);

        protected override JsonConverter ResolveContractConverter(Type objectType) {
            if (objectType == null)
                return base.ResolveContractConverter(objectType);
            var type = converterMapping.Keys.FirstOrDefault(x => x.IsAssignableFrom(objectType));
            return type == null ? base.ResolveContractConverter(objectType) : converterMapping[type];
        }
    }

    public class IPAddressConverter : JsonConverter<IPAddress>
    {
        protected override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }

        protected override IPAddress ReadJson(JsonReader reader, IPAddress existingValue, JsonSerializer serializer) {
            var token = JToken.Load(reader);
            var value = token.Value<string>();
            return value == null ? default(IPAddress) : IPAddress.Parse(value);
        }
    }

    public class AbsoluteDirectoryPathConverter : JsonInheritedConverter<IAbsoluteDirectoryPath>
    {
        protected override void WriteJson(JsonWriter writer, IAbsoluteDirectoryPath value, JsonSerializer serializer) {
            writer.WriteValue(value?.ToString());
        }

        protected override IAbsoluteDirectoryPath ReadJson(JsonReader reader, IAbsoluteDirectoryPath existingValue,
            JsonSerializer serializer) {
            var token = JToken.Load(reader);
            return token.Value<string>().ToAbsoluteDirectoryPathNullSafe();
        }
    }

    public class AbsoluteFilePathConverter : JsonInheritedConverter<IAbsoluteFilePath>
    {
        protected override void WriteJson(JsonWriter writer, IAbsoluteFilePath value, JsonSerializer serializer) {
            writer.WriteValue(value?.ToString());
        }

        protected override IAbsoluteFilePath ReadJson(JsonReader reader, IAbsoluteFilePath existingValue,
            JsonSerializer serializer) {
            var token = JToken.Load(reader);
            return token.Value<string>().ToAbsoluteFilePathNullSafe();
        }
    }


    public class IPEndPointConverter : JsonConverter<IPEndPoint>
    {
        protected override void WriteJson(JsonWriter writer, IPEndPoint value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("Address");
            serializer.Serialize(writer, value.Address);
            writer.WritePropertyName("Port");
            writer.WriteValue(value.Port);
            writer.WriteEndObject();
        }

        protected override IPEndPoint ReadJson(JsonReader reader, IPEndPoint existingValue,
            JsonSerializer serializer) {
            var jo = JObject.Load(reader);
            var address = (jo["Address"] ?? jo["address"]).ToObject<IPAddress>(serializer);
            var port = (jo["Port"] ?? jo["port"]).Value<int>();
            return new IPEndPoint(address, port);
        }
    }

    public class LockedGlobalizationDateConverter : IsoDateTimeConverter
    {
        public LockedGlobalizationDateConverter() {
            // Use invariant culture to ignore system culture
            Culture = new CultureInfo("");
        }
    }

    public class ShortDateConverter : LockedGlobalizationDateConverter
    {
        public ShortDateConverter() {
            DateTimeFormat = "yyyy-MM-dd";
        }
    }

    public class UnderscoreContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName) => propertyName.ToUnderscore();
    }

    public abstract class JsonConverter<T> : JsonConverter
    {
        public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (!(value is T)) {
                throw new JsonSerializationException(string.Format("This converter cannot convert {1} of type {0}",
                    value, value.GetType()));
            }
            WriteJson(writer, (T) value, serializer);
        }

        protected abstract void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);

        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            if (objectType != typeof (T))
                throw new JsonSerializationException($"This converter cannot convert type {objectType}");
            if (existingValue != null && !(existingValue is T)) {
                throw new JsonSerializationException(
                    string.Format("This converter cannot convert {1} of type {0}, but {2}",
                        existingValue, existingValue?.GetType(), typeof (T)));
            }
            return ReadJson(reader, (T) existingValue, serializer);
        }

        protected abstract T ReadJson(JsonReader reader, T existingValue, JsonSerializer serializer);

        public sealed override bool CanConvert(Type objectType) => typeof (T) == objectType;
    }

    public abstract class JsonInheritedConverter<T> : JsonConverter
    {
        public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (!(value is T)) {
                throw new JsonSerializationException(string.Format("This converter cannot convert {1} of type {0}",
                    value, value.GetType()));
            }
            WriteJson(writer, (T) value, serializer);
        }

        protected abstract void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);

        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            if (!CanConvert(objectType))
                throw new JsonSerializationException($"This converter cannot convert type {objectType}");
            if (existingValue != null && !(existingValue is T)) {
                throw new JsonSerializationException(
                    string.Format("This converter cannot convert {1} of type {0}, but {2}",
                        existingValue, existingValue?.GetType(), typeof (T)));
            }
            return ReadJson(reader, (T) existingValue, serializer);
        }

        protected abstract T ReadJson(JsonReader reader, T existingValue, JsonSerializer serializer);

        public sealed override bool CanConvert(Type objectType) => typeof (T).IsAssignableFrom(objectType);
    }
}