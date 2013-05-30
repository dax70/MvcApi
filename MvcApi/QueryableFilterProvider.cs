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

    public class QueryableFilterProvider : IFilterProvider
    {
        private QueryValidator validator;

        public QueryableFilterProvider()
        {
        }

        public QueryValidator Validator
        {
            get
            {
                if (validator == null)
                {
                    validator = GlobalConfiguration.Configuration.Services.GetQueryValidator();
                }
                return validator;
            }
            set { this.validator = value; }
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

        protected virtual IEnumerable<Filter> GetQueryableFilters(ControllerContext controllerContext, ApiActionDescriptor actionDescriptor)
        {
            yield return new Filter(new QueryFilterAttribute(this.Validator) { ResultLimit = 10, InlineCount = true }, FilterScope.Last, null);
            yield return new Filter(new QueryOfTypeFilterAttribute(), FilterScope.Last, null);
        }

        private static Type GetQueryElementTypeOrNull(Type returnType)
        {
            returnType = TypeHelper.GetUnderlyingContentInnerType(returnType);
            return QueryTypeHelper.GetQueryableInterfaceInnerTypeOrNull(returnType);
        }
    }
}
