#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http.OData.Query;
using System.Web.Mvc;
using MvcApi.Http;
using MvcApi.OData.Query; 
#endregion

namespace MvcApi.OData
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "We want to be able to subclass this type.")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class QueryableAttribute : ActionFilterAttribute
    {
        private const char CommaSeparator = ',';
        private const int MinAnyAllExpressionDepth = 0;
        private const int MinMaxTop = 0;
        private const int MinMaxSkip = 0;

        private HandleNullPropagationOption _handleNullPropagationOption = HandleNullPropagationOption.Default;
        private int _maxAnyAllExpressionDepth = 1;
        private int? _pageSize;

        private ODataValidationSettings _validationSettings;
        private string _allowedOrderByProperties;

        /// <summary>
        /// Enables a controller action to support OData query parameters.
        /// </summary>
        public QueryableAttribute()
        {
            EnsureStableOrdering = true;
            _validationSettings = new ODataValidationSettings();
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether query composition should
        /// alter the original query when necessary to ensure a stable sort order.
        /// </summary>
        /// <value>A <c>true</c> value indicates the original query should
        /// be modified when necessary to guarantee a stable sort order.
        /// A <c>false</c> value indicates the sort order can be considered
        /// stable without modifying the query.  Query providers that ensure
        /// a stable sort order should set this value to <c>false</c>.
        /// The default value is <c>true</c>.</value>
        public bool EnsureStableOrdering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how null propagation should
        /// be handled during query composition. 
        /// </summary>
        /// <value>
        /// The default is <see cref="HandleNullPropagationOption.Default"/>.
        /// </value>
        public HandleNullPropagationOption HandleNullPropagation
        {
            get
            {
                return _handleNullPropagationOption;
            }
            set
            {
                HandleNullPropagationOptionHelper.Validate(value, "value");
                _handleNullPropagationOption = value;
            }
        }

        ///// <summary>
        ///// Gets or sets the maximum depth of the Any or All elements nested inside the query.
        ///// </summary>
        ///// <remarks>
        ///// This limit helps prevent Denial of Service attacks. The default value is 1.
        ///// </remarks>
        ///// <value>
        ///// The maxiumum depth of the Any or All elements nested inside the query.
        ///// </value>
        //public int MaxAnyAllExpressionDepth
        //{
        //    get
        //    {
        //        return _maxAnyAllExpressionDepth;
        //    }
        //    set
        //    {
        //        if (value < MinAnyAllExpressionDepth)
        //        {
        //            throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, MinAnyAllExpressionDepth);
        //        }

        //        _maxAnyAllExpressionDepth = value;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the maximum number of query results to send back to clients.
        ///// </summary>
        ///// <value>
        ///// The maximum number of query results to send back to clients.
        ///// </value>
        //public int PageSize
        //{
        //    get
        //    {
        //        return _pageSize ?? default(int);
        //    }
        //    set
        //    {
        //        if (value <= 0)
        //        {
        //            throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 1);
        //        }
        //        _pageSize = value;
        //    }
        //}

        public AllowedQueryOptions AllowedQueryOptions
        {
            get
            {
                return _validationSettings.AllowedQueryOptions;
            }
            set
            {
                _validationSettings.AllowedQueryOptions = value;
            }
        }

        public AllowedFunctionNames AllowedFunctionsNames
        {
            get
            {
                return _validationSettings.AllowedFunctionNames;
            }
            set
            {
                _validationSettings.AllowedFunctionNames = value;
            }
        }

        public AllowedArithmeticOperators AllowedArithmeticOperators
        {
            get
            {
                return _validationSettings.AllowedArithmeticOperators;
            }
            set
            {
                _validationSettings.AllowedArithmeticOperators = value;
            }
        }

        public AllowedLogicalOperators AllowedLogicalOperators
        {
            get
            {
                return _validationSettings.AllowedLogicalOperators;
            }
            set
            {
                _validationSettings.AllowedLogicalOperators = value;
            }
        }

        public string AllowedOrderByProperties
        {
            get
            {
                return _allowedOrderByProperties;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Argument Null or Empty");
                }

                _allowedOrderByProperties = value;

                // now parse the value and set it to validationSettings
                string[] properties = _allowedOrderByProperties.Split(CommaSeparator);
                for (int i = 0; i < properties.Length; i++)
                {
                    _validationSettings.AllowedOrderByProperties.Add(properties[i].Trim());
                }
            }
        }

        public int MaxSkip
        {
            get
            {
                return _validationSettings.MaxSkip ?? default(int);
            }
            set
            {
                if (value < MinMaxSkip)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Argument value = {0} must be greater than or equal to {1}", value, MinMaxSkip));
                }

                _validationSettings.MaxSkip = value;
            }
        }

        //public int MaxTop
        //{
        //    get
        //    {
        //        return _validationSettings.MaxTop ?? default(int);
        //    }
        //    set
        //    {
        //        if (value < MinMaxTop)
        //        {
        //            throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, MinMaxTop);
        //        }

        //        _validationSettings.MaxTop = value;
        //    }
        //}

#endregion 

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
        }

        internal static bool IsSupportedReturnType(Type objectType)
        {
            Contract.Assert(objectType != null);

            if (objectType == typeof(IEnumerable) || objectType == typeof(IQueryable))
            {
                return true;
            }

            if (objectType.IsGenericType)
            {
                Type genericTypeDefinition = objectType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(IEnumerable<>) || genericTypeDefinition == typeof(IQueryable<>))
                {
                    return true;
                }
            }

            return false;
        }

        //[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Response disposed after being sent.")]
        //private IQueryable ExecuteQuery(IEnumerable query, HttpRequestMessage request, HttpConfiguration configuration, HttpActionDescriptor actionDescriptor)
        //{
        //    Type originalQueryType = query.GetType();
        //    Type elementClrType = TypeHelper.GetImplementedIEnumerableType(originalQueryType);

        //    if (elementClrType == null)
        //    {
        //        // The element type cannot be determined because the type of the content
        //        // is not IEnumerable<T> or IQueryable<T>.
        //        throw Error.InvalidOperation(
        //            SRResources.FailedToRetrieveTypeToBuildEdmModel,
        //            this.GetType().Name,
        //            actionDescriptor.ActionName,
        //            actionDescriptor.ControllerDescriptor.ControllerName,
        //            originalQueryType.FullName);
        //    }

        //    ODataQueryContext queryContext = CreateQueryContext(elementClrType, configuration, actionDescriptor);
        //    ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, request);
        //    ValidateQuery(request, queryOptions);

        //    // apply the query
        //    IQueryable queryable = query as IQueryable;
        //    if (queryable == null)
        //    {
        //        queryable = query.AsQueryable();
        //    }

        //    ODataQuerySettings querySettings = new ODataQuerySettings
        //    {
        //        EnsureStableOrdering = EnsureStableOrdering,
        //        HandleNullPropagation = HandleNullPropagation,
        //        MaxAnyAllExpressionDepth = MaxAnyAllExpressionDepth,
        //        ResultLimit = _pageSize
        //        //PageSize = _pageSize
        //    };

        //    return queryOptions.ApplyTo(queryable, querySettings);
        //}

        //internal static ODataQueryContext CreateQueryContext(Type elementClrType, HttpConfiguration configuration, HttpActionDescriptor actionDescriptor)
        //{
        //    // Get model for the entire app
        //    IEdmModel model = configuration.GetEdmModel();

        //    if (model == null || model.GetEdmType(elementClrType) == null)
        //    {
        //        // user has not configured anything or has registered a model without the element type
        //        // let's create one just for this type and cache it in the action descriptor
        //        model = actionDescriptor.GetEdmModel(elementClrType);
        //        Contract.Assert(model != null);
        //    }

        //    // parses the query from request uri
        //    return new ODataQueryContext(model, elementClrType);
        //}

        /// <summary>
        /// Validates that the OData query of the incoming request is supported.
        /// </summary>
        /// <param name="request">The incoming request</param>
        /// <param name="queryOptions">The <see cref="ODataQueryOptions"/> instance constructed based on the incoming request.</param>
        /// <remarks>
        /// Override this method to perform additional validation of the query. By default, the implementation
        /// throws an exception if the query contains unsupported query parameters.
        /// </remarks>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Response disposed after being sent.")]
        public virtual void ValidateQuery(HttpRequestMessage request, ODataQueryOptions queryOptions)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (queryOptions == null)
            {
                throw new ArgumentNullException("queryOptions");
            }

            foreach (string key in request.QueryString.Keys)
            {
                if (!ODataQueryOptions.IsSupported(key) && key.StartsWith("$", StringComparison.Ordinal))
                {
                //        // we don't support any custom query options that start with $
                //        throw new HttpException(request.CreateErrorResponse(HttpStatusCode.BadRequest,
                //            Error.Format(SRResources.QueryParameterNotSupported, kvp.Key)));
                }
            }

            queryOptions.Validate(_validationSettings);
        }
    }
}
