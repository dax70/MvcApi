using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using MvcApi.Formatting;
using MvcApi.Properties;
using MvcApi.Query;

namespace MvcApi
{    
    /// <summary>
    /// This provides a centralized list of type-safe accessors describing where and how we get services.
    /// This also provides a single entry point for each service request. That makes it easy
    /// to see which parts of the code use it, and provides a single place to comment usage.
    /// Accessors encapsulate usage like:
    /// <list type="bullet">
    /// <item>Type-safe using {T} instead of unsafe <see cref="System.Type"/>.</item>
    /// <item>which type do we key off? This is interesting with type hierarchies.</item>
    /// <item>do we ask for singular or plural?</item>
    /// <item>is it optional or mandatory?</item>
    /// <item>what are the ordering semantics</item>
    /// </list>
    /// Expected that any <see cref="IEnumerable{T}"/> we return is non-null, although possibly empty.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ServicesExtensions
    {
        public static IActionInvoker GetActionInvoker(this ServicesContainer services)
        {
            return services.GetServiceOrThrow<IActionInvoker>();
        }

        public static IActionSelector GetActionSelector(this ServicesContainer services)
        {
            return services.GetServiceOrThrow<IActionSelector>();
        }

        public static IContentNegotiator GetContentNegotiator(this ServicesContainer services)
        {
            return services.GetService<IContentNegotiator>();
        }

        public static QueryValidator GetQueryValidator(this ServicesContainer services)
        {
            return services.GetServiceOrThrow<QueryValidator>();
        }

        public static IStructuredQuerySource GetStructuredQueryFactory(this ServicesContainer services)
        {
            return services.GetService<IStructuredQuerySource>();
        }

        public static IStructuredQueryBuilder GetStructuredQueryBuilder(this ServicesContainer services)
        {
            return services.GetService<IStructuredQueryBuilder>();
        }

        public static IViewSelector GetViewSelector(this ServicesContainer services)
        {
            return services.GetServiceOrThrow<IViewSelector>();
        }

        // Runtime code shouldn't call GetService() directly. Instead, have a wrapper (like the ones above) and call through the wrapper.
        private static TService GetService<TService>(this ServicesContainer services)
        {
            if (services == null)
            {
                throw Error.ArgumentNull("services");
            }

            return (TService)services.GetService(typeof(TService));
        }

        private static IEnumerable<TService> GetServices<TService>(this ServicesContainer services)
        {
            if (services == null)
            {
                throw Error.ArgumentNull("services");
            }

            return services.GetServices(typeof(TService)).Cast<TService>();
        }

        private static T GetServiceOrThrow<T>(this ServicesContainer services)
        {
            T result = services.GetService<T>();
            if (result == null)
            {
                throw Error.InvalidOperation(SRResources.DependencyResolverNoService, typeof(T).FullName);
            }

            return result;
        }
    }
}
