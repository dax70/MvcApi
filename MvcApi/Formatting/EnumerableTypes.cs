namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MvcApi.Collections;
    #endregion

    internal static class EnumerableTypes
    {
        public static readonly Type DelegatingEnumerableGenericType = typeof(DelegatingEnumerable<>);

        public static readonly Type QueryableWrapperGenericType = typeof(QueryableWrapper<>);

        public static readonly Type EnumerableInterfaceGenericType = typeof(IEnumerable<>);

        public static readonly Type QueryableInterfaceGenericType = typeof(IQueryable<>);
    }
}
