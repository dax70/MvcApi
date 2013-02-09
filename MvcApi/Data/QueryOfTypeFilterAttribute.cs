#region Using Directives
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Mvc;
using MvcApi; 
#endregion

namespace MvcApi.Data
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class QueryOfTypeFilterAttribute : ActionFilterAttribute
    {
        public QueryOfTypeFilterAttribute()
        {
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            Contract.Assert(filterContext.HttpContext != null);

            var request = filterContext.HttpContext.Request;
            var result = filterContext.Result;

            IQueryable source;

            if (result != null && result.TryGetObjectValue<IQueryable>(out source))
            {
                if (request != null && request.QueryString != null && request.QueryString.Count > 0)
                {
                    string typeName = request.QueryString["oftype"];
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        Type type = ResolveType(typeName, source.ElementType);
                        if (type != null)
                        {
                            ((ObjectContent)result).Value = QueryOfType(source, type);
                        }
                    }
                }
            }
        }

        private static IQueryable QueryOfType(IQueryable source, Type type)
        {
            return source.Provider.CreateQuery(
                    Expression.Call(null,
                        typeof(Queryable).GetMethod("OfType").MakeGenericMethod(new Type[] { type }),
                        new Expression[] { source.Expression })
                    );
        }

        private static Type ResolveType(string typeName, Type baseType)
        {
            // Assume SubClass is on same assembly as most ORM's use partials, etc.
            Type type = baseType.Assembly.GetType(baseType.Namespace + "." + typeName, false /*throwOnError*/, true /*ignoreCase*/);
            // TODO: check is type is AssignableFrom
            //baseType.IsAssignableFrom(type);

            if (type == null && baseType.Assembly.Equals(Assembly.GetExecutingAssembly()))
            {
                type = Assembly.GetExecutingAssembly().GetType(typeName, false /*throwOnError*/, true /*ignoreCase*/);
            }
            return type;
        }
    }
}
