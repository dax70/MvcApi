using System;
using System.Collections.Generic;

namespace MvcApi.Formatting
{
    public interface IObjectConverter
    {
        bool CanConvertFrom(Type sourceType);

        object ConvertFrom(object value);
    }

    public class ObjectConverter<T> : IObjectConverter
    {
        public Func<T, object> Mapper { get; set; }

        public bool CanConvertFrom(Type sourceType)
        {
            return true; // TODO: verification logic
        }

        public object ConvertFrom(object value)
        {
            T original = (T)value;
            return Mapper(original);
        }
    }

    public static class JsonContractFormatter
    {
        private static Dictionary<Type, IObjectConverter> converters = new Dictionary<Type, IObjectConverter>();

        public static void AddConverter(Type type, IObjectConverter converter)
        {
            converters.Add(type, converter);
        }

        public static IObjectConverter GetConverter(Type type)
        {
            return converters[type];
        }
    }
}
