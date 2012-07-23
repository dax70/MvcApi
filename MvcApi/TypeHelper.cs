namespace MvcApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal static class TypeHelper
    {
        internal static readonly Type ApiControllerType = ApiControllerType = typeof(ApiController);

        static TypeHelper()
        {
        }

        internal static bool IsNullableSimpleType(Type type)
        {
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                return IsSimpleType(nullable);
            }
            return IsSimpleType(type);
        }

        internal static bool IsSimpleType(Type type)
        {
            if (((!type.IsPrimitive && !type.Equals(typeof(string)))
                && (!type.Equals(typeof(DateTime)) && !type.Equals(typeof(decimal))))
                && (!type.Equals(typeof(Guid)) && !type.Equals(typeof(DateTimeOffset))))
            {
                return type.Equals(typeof(TimeSpan));
            }
            return true;
        }

        internal static bool IsTypeNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        internal static ReadOnlyCollection<T> OfType<T>(object[] objects) where T : class
        {
            int length = objects.Length;
            List<T> list = new List<T>(length);
            int itemCount = 0;
            for (int i = 0; i < length; i++)
            {
                T item = objects[i] as T;
                if (item != null)
                {
                    list.Add(item);
                    itemCount++;
                }
            }
            list.Capacity = itemCount;
            return new ReadOnlyCollection<T>(list);
        }
    }
}
