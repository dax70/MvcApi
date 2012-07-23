namespace MvcApi//System.Web.Mvc
{
    #region Using Directives
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using System.Web.Mvc; 
    #endregion

    internal static class ReflectedAttributeCache
    {
        #region Fields        
        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<ActionMethodSelectorAttribute>> _actionMethodSelectorAttributeCache;
        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<ActionNameSelectorAttribute>> _actionNameSelectorAttributeCache;
        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<FilterAttribute>> _methodFilterAttributeCache;
        private static readonly ConcurrentDictionary<Type, ReadOnlyCollection<FilterAttribute>> _typeFilterAttributeCache; 
        #endregion

        static ReflectedAttributeCache()
        {
            _actionMethodSelectorAttributeCache = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<ActionMethodSelectorAttribute>>();
            _actionNameSelectorAttributeCache = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<ActionNameSelectorAttribute>>();
            _methodFilterAttributeCache = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<FilterAttribute>>();
            _typeFilterAttributeCache = new ConcurrentDictionary<Type, ReadOnlyCollection<FilterAttribute>>();
        }

        public static ICollection<ActionMethodSelectorAttribute> GetActionMethodSelectorAttributes(MethodInfo methodInfo)
        {
            return GetAttributes<MethodInfo, ActionMethodSelectorAttribute>(_actionMethodSelectorAttributeCache, methodInfo);
        }

        public static ICollection<ActionNameSelectorAttribute> GetActionNameSelectorAttributes(MethodInfo methodInfo)
        {
            return GetAttributes<MethodInfo, ActionNameSelectorAttribute>(_actionNameSelectorAttributeCache, methodInfo);
        }

        public static ICollection<FilterAttribute> GetMethodFilterAttributes(MethodInfo methodInfo)
        {
            return GetAttributes<MethodInfo, FilterAttribute>(_methodFilterAttributeCache, methodInfo);
        }

        public static ICollection<FilterAttribute> GetTypeFilterAttributes(Type type)
        {
            return GetAttributes<Type, FilterAttribute>(_typeFilterAttributeCache, type);
        }

        private static ReadOnlyCollection<TAttribute> GetAttributes<TMemberInfo, TAttribute>(ConcurrentDictionary<TMemberInfo, ReadOnlyCollection<TAttribute>> lookup, TMemberInfo memberInfo)
            where TMemberInfo : MemberInfo
            where TAttribute : Attribute
        {
            return lookup.GetOrAdd(memberInfo, delegate(TMemberInfo mi)
            {
                bool inherit = true;
                return new ReadOnlyCollection<TAttribute>((TAttribute[])memberInfo.GetCustomAttributes(typeof(TAttribute), inherit));
            });
        }
    }
}