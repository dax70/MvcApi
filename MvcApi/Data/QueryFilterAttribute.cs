namespace MvcApi.Data
{
    #region Using Directives
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Web.Mvc;
    using System.Web;
    using MvcApi.Query;
    #endregion

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class QueryFilterAttribute : QueryableFilterAttribute
    {
        internal static readonly string TotalCountKey = "MvcApi_InlineCountKey";
        private static readonly MethodInfo _getTotalCountMethod = typeof(QueryFilterAttribute).GetMethod("GetTotalCount", BindingFlags.NonPublic | BindingFlags.Instance);

        public QueryFilterAttribute()
        {
        }

        public QueryFilterAttribute(QueryValidator queryValidator)
            : base(queryValidator)
        {
        }

        public bool InlineCount { get; set; }

        protected override IQueryable ApplyResultLimit(ActionExecutedContext actionExecutedContext, IQueryable query)
        {
            if (actionExecutedContext == null)
            {
                throw Error.ArgumentNull("actionExecutedContext");
            }
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }
            bool inlineCountRequested = InlineCount || ShouldInlineCount(actionExecutedContext.HttpContext.Request);

            ActionResult result = actionExecutedContext.Result;
            if (result != null && inlineCountRequested && query != null)
            {
                // Compute the total count and add the result as a request property. Later after all
                // filters have run, DataController will transform the final result into a QueryResult
                // which includes this value.
                // TODO : use a compiled/cached delegate?
                int totalCount = (int)_getTotalCountMethod.MakeGenericMethod(query.ElementType).Invoke(this, new object[] { query });
                actionExecutedContext.HttpContext.Items.Add(TotalCountKey, totalCount);
            }
            return base.ApplyResultLimit(actionExecutedContext, query);
        }

        /// <summary>
        /// Inspects the specified query and if the query has any paging operators
        /// at the end of it (either a single Take or a Skip/Take) the underlying
        /// query w/o the Skip/Take is returned.
        /// </summary>
        /// <param name="query">The query to inspect.</param>
        /// <param name="countQuery">The resulting count query. Null if there is no paging.</param>
        /// <returns>True if a count query is returned, false otherwise.</returns>
        internal static bool TryRemovePaging(IQueryable query, out IQueryable countQuery)
        {
            MethodCallExpression mce = query.Expression as MethodCallExpression;
            Expression countExpr = null;

            // TODO what if the paging does not follow the exact Skip().Take() pattern?
            // Check for skip incase Take is applied as a Limit.
            if (IsSequenceOperator("take", mce) || IsSequenceOperator("skip", mce)) 
            {
                // strip off the Take operator
                countExpr = mce.Arguments[0];

                mce = countExpr as MethodCallExpression;
                if (IsSequenceOperator("skip", mce))
                {
                    // If there's a skip then we need to exclude that too. No skip means we're 
                    // on the first page.
                    countExpr = mce.Arguments[0];
                }
            }

            countQuery = null;
            if (countExpr != null)
            {
                countQuery = query.Provider.CreateQuery(countExpr);
                return true;
            }

            return false;
        }

        private int GetTotalCount<T>(IQueryable<T> results)
        {
            // A total count of -1 indicates that the count is the result count. This
            // is the default, unless we discover that skip/top operations will be
            // performed, in which case we'll form and execute a count query.
            int totalCount = -1;

            IQueryable totalCountQuery = null;
            if (TryRemovePaging(results, out totalCountQuery))
            {
                totalCount = ((IQueryable<T>)totalCountQuery).Count();
            }
            else if (ResultLimit > 0)
            {
                // The client query didn't specify any skip/top paging operations.
                // However, this action has a ResultLimit applied.
                // Therefore, we need to take the count now before that limit is applied.
                totalCount = results.Count();
            }

            return totalCount;
        }

        private static bool IsSequenceOperator(string operatorName, MethodCallExpression mce)
        {
            return (((mce != null) && (mce.Method.DeclaringType == typeof(Queryable))) && mce.Method.Name.Equals(operatorName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldInlineCount(HttpRequestBase request)
        {
            if (request != null && request.QueryString != null && request.QueryString.Count > 0)
            {
                string countValue = request.QueryString["$inlinecount"];
                return (countValue == "allpages");
            }
            return false;
        }
    }
}
