namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Web.Mvc;
    using MvcApi.Http;
    using MvcApi.Query;
    using MvcApi.Properties;
    #endregion

    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "We want to be able to subclass this type")]
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class QueryableFilterAttribute : ActionFilterAttribute
    {
        private readonly QueryValidator _queryValidator;
        private IStructuredQuerySource _structuredQueryFactory;
        private IStructuredQueryBuilder _structuredQueryBuilder;

        private static readonly IStructuredQuerySource _defaultQueryFactory = new DefaultStructuredQuerySource();
        private static readonly IStructuredQueryBuilder _defaultQueryBuilder = new DefaultStructuredQueryBuilder();

        public QueryableFilterAttribute()
            : this(QueryValidator.Instance)
        {
        }

        public QueryableFilterAttribute(QueryValidator queryValidator)
        {
            this._queryValidator = queryValidator;
        }

        /// <summary>
        /// The maximum number of results that should be returned from this query regardless of query-specified limits. A value of <c>0</c>
        /// indicates no limit. Negative values are not supported and will cause a runtime exception.
        /// </summary>
        public int ResultLimit { get; set; }

        /// <summary>
        /// The <see cref="IStructuredQuerySource"/> to use. Derived classes can use this to have a per-attribute query factory 
        /// instead of the one on <see cref="Configuration"/>
        /// </summary>
        protected IStructuredQuerySource StructuredQuerySource
        {
            get { return _structuredQueryFactory; }
            set { _structuredQueryFactory = value; }
        }

        /// <summary>
        /// The <see cref="IStructuredQueryBuilder"/> to use. Derived classes can use this to have a per-attribute query builder 
        /// instead of the one on <see cref="Configuration"/>
        /// </summary>
        protected IStructuredQueryBuilder StructuredQueryBuilder
        {
            get { return _structuredQueryBuilder; }
            set { _structuredQueryBuilder = value; }
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext == null)
            {
                throw Error.ArgumentNull("actionContext");
            }
            if (this.ResultLimit <= 0)
            {
                Error.ArgumentOutOfRange("resultLimit", this.ResultLimit, SRResources.ResultLimitFilter_OutOfRange, actionContext.ActionDescriptor.ActionName);
            }
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null)
            {
                throw Error.ArgumentNull("actionExecutedContext");
            }

            Contract.Assert(actionExecutedContext.HttpContext != null);

            var request = actionExecutedContext.HttpContext.Request;
            var result = actionExecutedContext.Result;

            IQueryable query;
            if (result != null && result.TryGetObjectValue<IQueryable>(out query))
            {
                IQueryable deserializedQuery = null;
                if (request != null && request.QueryString != null && request.QueryString.Count > 0)
                {
                    try
                    {
                        StructuredQuery structuredQuery = GetStructuredQuery(request);

                        if (structuredQuery != null && structuredQuery.QueryParts.Any())
                        {
                            IQueryable baseQuery = Array.CreateInstance(query.ElementType, 0).AsQueryable(); // T[]
                            deserializedQuery = GetDeserializedQuery(baseQuery, structuredQuery);
                            if (_queryValidator != null && deserializedQuery != null)
                            {
                                _queryValidator.Validate(deserializedQuery);
                            }
                        }
                    }
                    catch (ParseException exception)
                    {
                        throw new HttpException(/*BadRequest*/400, SR.UriQueryStringInvalid, exception);
                    }
                }

                if (deserializedQuery != null)
                {
                    query = QueryComposer.Compose(query, deserializedQuery);
                }

                query = ApplyResultLimit(actionExecutedContext, query);

                ((ObjectContent)result).Value = query;
            }
        }

        protected virtual IQueryable GetDeserializedQuery(IQueryable query, StructuredQuery structuredQery)
        {
            Contract.Assert(query != null);
            Contract.Assert(structuredQery != null);

            IStructuredQueryBuilder queryBuilder = null;
            if (StructuredQuerySource != null)
            {
                queryBuilder = StructuredQueryBuilder;
            }
            else
            {
                Configuration configuration = GlobalConfiguration.Configuration; 
                if (configuration != null)
                {
                    queryBuilder = configuration.Services.GetStructuredQueryBuilder();
                }
            }

            queryBuilder = queryBuilder ?? _defaultQueryBuilder;
            return queryBuilder.ApplyQuery(query, structuredQery.QueryParts);
        }

        protected virtual IQueryable ApplyResultLimit(ActionExecutedContext actionExecutedContext, IQueryable query)
        {
            if (actionExecutedContext == null)
            {
                throw Error.ArgumentNull("actionExecutedContext");
            }
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            if (ResultLimit > 0)
            {
                query = query.Take(ResultLimit);
            }
            return query;
        }

        private StructuredQuery GetStructuredQuery(HttpRequestBase request)
        {
            Contract.Assert(request != null);

            IStructuredQuerySource queryFactory = null;
            if (StructuredQuerySource != null)
            {
                queryFactory = StructuredQuerySource;
            }
            else
            {
                Configuration configuration = GlobalConfiguration.Configuration; //request.GetConfiguration();
                if (configuration != null)
                {
                    queryFactory = configuration.Services.GetStructuredQueryFactory();
                }
            }

            queryFactory = queryFactory ?? _defaultQueryFactory;
            return queryFactory.CreateQuery(request.Url);
        }
    }
}

