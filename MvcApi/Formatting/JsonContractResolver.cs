using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MvcApi.Formatting
{
    // Default Contract resolver for JsonMediaTypeFormatter
    // Uses the IRequiredMemberSelector to choose required members
    internal class JsonContractResolver : DefaultContractResolver
    {
        private readonly MediaTypeFormatter _formatter;

        public JsonContractResolver(MediaTypeFormatter formatter)
        {
            this._formatter = formatter;
            // Need this setting to have [Serializable] types serialized correctly
            IgnoreSerializableAttribute = false;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            this.ConfigureProperty(member, property);
            return property;
        }

        private void ConfigureProperty(MemberInfo member, JsonProperty property)
        {
            if (this._formatter.RequiredMemberSelector != null && this._formatter.RequiredMemberSelector.IsRequiredMember(member))
            {
                property.Required = Required.AllowNull;
                property.DefaultValueHandling = DefaultValueHandling.Include;
                property.NullValueHandling = NullValueHandling.Include;
                return;
            }
            property.Required = Required.Default;
            property.DefaultValueHandling = DefaultValueHandling.Ignore;
            property.NullValueHandling = NullValueHandling.Ignore;
        }

        private static bool IsTypeDataContract(Type type)
        {
            return type.GetCustomAttributes(typeof(DataContractAttribute), false).Any<object>();
        }

        private static bool IsTypeJsonObject(Type type)
        {
            return type.GetCustomAttributes(typeof(JsonObjectAttribute), false).Any<object>();
        }

    }
}
