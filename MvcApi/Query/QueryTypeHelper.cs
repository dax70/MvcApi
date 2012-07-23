namespace MvcApi.Query
{
    using System;
    using System.Linq;

    internal static class QueryTypeHelper
    {
        internal static readonly Type QueryableInterfaceGenericType = typeof(IQueryable<>);

        internal static Type GetQueryableInterfaceInnerTypeOrNull(Type type)
        {
            if (type == null)
            {
                return type;
            }
            if (IsQueryableInterfaceGenericType(type))
            {
                return type.GetGenericArguments()[0];
            }
            if (ImplementsQueryableInterfaceGenericType(type))
            {
                return type.GetInterface(QueryableInterfaceGenericType.FullName).GetGenericArguments()[0];
            }
            return null;
        }

        private static bool ImplementsQueryableInterfaceGenericType(Type type)
        {
            return (type.GetInterface(QueryableInterfaceGenericType.FullName) != null);
        }

        private static bool IsQueryableInterfaceGenericType(Type type)
        {
            return ((type.IsInterface && type.IsGenericType) && type.GetGenericTypeDefinition().Equals(QueryableInterfaceGenericType));
        }

        internal static bool IsQueryableInterfaceGenericTypeOrImplementation(Type type)
        {
            if (type == null)
            {
                return false;
            }
            if (!IsQueryableInterfaceGenericType(type))
            {
                return ImplementsQueryableInterfaceGenericType(type);
            }
            return true;
        }
    }
}

