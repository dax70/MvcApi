namespace MvcApi.Query
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using MvcApi.Properties;
    using MvcApi.Http;

    internal static class TypeHelper
    {
        private static readonly Type HttpResponseMessageGenericType;
        private static readonly Type ObjectContentGenericType;
        private static readonly Type TaskGenericType;

        static TypeHelper()
        {
            TaskGenericType = typeof(Task<>);
            ObjectContentGenericType = typeof(ObjectContent<>);
            HttpResponseMessageGenericType = typeof(HttpResponseMessage<>);
        }

        internal static Type GetHttpResponseInnerTypeOrNull(Type type)
        {
            if ((!type.IsGenericType || type.IsGenericTypeDefinition) || !IsHttpResponseGenericTypeDefinition(type.GetGenericTypeDefinition()))
            {
                return null;
            }
            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length > 1)
            {
                throw Error.InvalidOperation(SR.MultipleTypeParametersForHttpContentType, type.Name);
            }
            return genericArguments[0];
        }

        internal static Type GetHttpResponseOrContentInnerTypeOrNull(Type type)
        {
            if ((!type.IsGenericType || type.IsGenericTypeDefinition) || !IsHttpResponseOrContentGenericTypeDefinition(type.GetGenericTypeDefinition()))
            {
                return null;
            }
            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length > 1)
            {
                throw Error.InvalidOperation(SR.MultipleTypeParametersForHttpContentType, type.Name);
            }
            return genericArguments[0];
        }

        internal static Type GetUnderlyingContentInnerType(Type type)
        {
            Type type2 = GetTaskInnerTypeOrNull(type) ?? type;
            return (GetHttpResponseOrContentInnerTypeOrNull(type2) ?? type2);
        }

        internal static Type GetTaskInnerTypeOrNull(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (TaskGenericType == genericTypeDefinition)
                {
                    return type.GetGenericArguments()[0];
                }
            }
            return null;
        }

        internal static bool IsHttpResponseGenericTypeDefinition(Type type)
        {
            return (type.IsGenericTypeDefinition && HttpResponseMessageGenericType.IsAssignableFrom(type));
        }

        internal static bool IsHttpResponseOrContentGenericTypeDefinition(Type type)
        {
            if (!type.IsGenericTypeDefinition || (!HttpResponseMessageGenericType.IsAssignableFrom(type) && !ObjectContentGenericType.IsAssignableFrom(type)))
            {
                return false;
            }
            return true;
        }
    }
}
