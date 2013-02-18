﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Query;
using System.Web.Mvc;

namespace MvcApi.OData.Query
{
    /// <summary>
    /// An implementation of <see cref="IFilterProvider" /> that applies an action filter to
    /// any action with an <see cref="IQueryable" /> or <see cref="IQueryable{T}" /> return type
    /// that doesn't bind a parameter of type <see cref="ODataQueryOptions" />.
    /// </summary>
    public class QueryFilterProvider : IFilterProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterProvider" /> class.
        /// </summary>
        /// <param name="queryFilter">The action filter that executes the query.</param>
        public QueryFilterProvider(IActionFilter queryFilter)
        {
            if (queryFilter == null)
            {
                throw new ArgumentNullException("queryFilter");
            }

            QueryFilter = queryFilter;
        }

        /// <summary>
        /// Gets the action filter that executes the query.
        /// </summary>
        public IActionFilter QueryFilter { get; private set; }

        ///// <summary>
        ///// Provides filters to apply to the specified action.
        ///// </summary>
        ///// <param name="controllerContext">The current controllerContext.</param>
        ///// <param name="actionDescriptor">The action descriptor for the action to provide filters for.</param>
        ///// <returns>
        ///// The filters to apply to the specified action.
        ///// </returns>
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            // Actions with a bound parameter of type ODataQueryOptions do not support the query filter
            // The assumption is that the action will handle the querying within the action implementation
            var apiActionDescriptor = actionDescriptor as ApiActionDescriptor;
            if (apiActionDescriptor != null && IsIQueryable(apiActionDescriptor.ReturnType) &&
                !actionDescriptor.GetParameters().Any(parameter => typeof(ODataQueryOptions).IsAssignableFrom(parameter.ParameterType)))
            {
                return new Filter[] { new Filter(QueryFilter, FilterScope.Global, null) };
            }

            return Enumerable.Empty<Filter>();
        }

        internal static bool IsIQueryable(Type type)
        {
            return type == typeof(IQueryable) ||
                (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));
        }
    }
}
