namespace MvcApi.Filters
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using MvcApi.Data;
    using MvcApi.Query;
    #endregion

    public abstract class QueryableFilterProvider : IFilterProvider
    {
        private QueryValidator validator;

        public QueryableFilterProvider()
        {
        }

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var apiActionDescriptor = actionDescriptor as ApiActionDescriptor;
            if (apiActionDescriptor != null && apiActionDescriptor.ReturnType != null)
            {
                Type queryElementTypeOrNull = GetQueryElementTypeOrNull(apiActionDescriptor.ReturnType);
                if (queryElementTypeOrNull != null)
                {
                    return GetQueryableFilters(controllerContext, apiActionDescriptor);
                }
            }
            return Enumerable.Empty<Filter>();
        }

        protected abstract IEnumerable<Filter> GetQueryableFilters(ControllerContext controllerContext, ApiActionDescriptor actionDescriptor);

        private static Type GetQueryElementTypeOrNull(Type returnType)
        {
            returnType = TypeHelper.GetUnderlyingContentInnerType(returnType);
            return QueryTypeHelper.GetQueryableInterfaceInnerTypeOrNull(returnType);
        }
    }
}
