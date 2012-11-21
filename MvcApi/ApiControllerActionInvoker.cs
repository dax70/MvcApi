namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Mvc.Async;
    using Microsoft.Web.Infrastructure.DynamicValidationHelper;
    using MvcApi.Formatting;
    using MvcApi.Http;
    #endregion

    public class ApiControllerActionInvoker : AsyncControllerActionInvoker
    {
        private ControllerDescriptorCache _instanceDescriptorCache;

        public ApiControllerActionInvoker()
        {
        }

        #region Properties

        internal ControllerDescriptorCache DescriptorCache
        {
            get
            {
                if (this._instanceDescriptorCache == null)
                {
                    this._instanceDescriptorCache = _staticDescriptorCache;
                }
                return this._instanceDescriptorCache;
            }
            set
            {
                this._instanceDescriptorCache = value;
            }
        }
        #endregion

        internal IAsyncResult BeginInvokeActionDescriptor(ControllerContext controllerContext, ActionDescriptor actionDescriptor, AsyncCallback callback, object state)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (actionDescriptor == null)
            {
                throw new ArgumentException("actionDescriptor");
            }
            if (actionDescriptor != null)
            {
                FilterInfo filterInfo = this.GetFilters(controllerContext, actionDescriptor);
                Action continuation = null;
                BeginInvokeDelegate beginDelegate = delegate(AsyncCallback asyncCallback, object asyncState)
                {
                    try
                    {
                        AuthorizationContext authContext = this.InvokeAuthorizationFilters(controllerContext, filterInfo.AuthorizationFilters, actionDescriptor);
                        if (authContext.Result == null)
                        {
                            if (controllerContext.Controller.ValidateRequest)
                            {
                                //Internal: recreate -> ControllerActionInvoker.ValidateRequest(controllerContext);
                                ValidateRequest(controllerContext);
                            }
                            IDictionary<string, object> parameterValues = this.GetParameterValues(controllerContext, actionDescriptor);
                            IAsyncResult asyncResult = this.BeginInvokeActionMethodWithFilters(controllerContext, filterInfo.ActionFilters, actionDescriptor, parameterValues, asyncCallback, asyncState);
                            continuation = delegate
                            {
                                ActionExecutedContext actionExecutedContext = this.EndInvokeActionMethodWithFilters(asyncResult);
                                this.InvokeActionResultWithFilters(controllerContext, filterInfo.ResultFilters, actionExecutedContext.Result);
                            };
                            return asyncResult;
                        }
                        continuation = delegate
                        {
                            this.InvokeActionResult(controllerContext, authContext.Result);
                        };
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        ExceptionContext exceptionContext = this.InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, exception);
                        if (!exceptionContext.ExceptionHandled)
                        {
                            throw;
                        }
                        continuation = delegate
                        {
                            this.InvokeActionResult(controllerContext, exceptionContext.Result);
                        };
                    }
                    return BeginInvokeAction_MakeSynchronousAsyncResult(asyncCallback, asyncState);
                };
                EndInvokeDelegate<bool> endDelegate = delegate(IAsyncResult asyncResult)
                {
                    try
                    {
                        continuation();
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        ExceptionContext exceptionContext = this.InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, exception);
                        if (!exceptionContext.ExceptionHandled)
                        {
                            throw;
                        }
                        this.InvokeActionResult(controllerContext, exceptionContext.Result);
                    }
                    return true;
                };
                return AsyncResultWrapper.Begin<bool>(callback, state, beginDelegate, endDelegate, _invokeActionTag);
            }
            return BeginInvokeAction_ActionNotFound(callback, state);
        }

        public bool EndInvokeActionDescriptor(IAsyncResult asyncResult)
        {
            return AsyncResultWrapper.End<bool>(asyncResult, _invokeActionTag);
        }

        protected override ActionResult CreateActionResult(ControllerContext controllerContext, ActionDescriptor actionDescriptor, object actionReturnValue)
        {
            // TODO think about Http Semantics (ex: NotFound)?
            if (actionReturnValue == null)
            {
                return new EmptyResult();
            }
            ActionResult actionResult = actionReturnValue as ActionResult;
            if (actionResult != null)
            {
                return actionResult;
            }
            if (actionReturnValue is string)
            {
                return new ContentResult { Content = Convert.ToString(actionReturnValue, CultureInfo.InvariantCulture) };
            }
            return RunContentNegotiation(controllerContext, actionDescriptor, actionReturnValue);
        }

        private static ActionResult RunContentNegotiation(ControllerContext controllerContext, ActionDescriptor actionDescriptor, object actionReturnValue)
        {
            var configuration = GlobalConfiguration.Configuration;
            IContentNegotiator contentNegotiator = configuration.Services.GetContentNegotiator();

            var requestMessage = HttpExtensions.ConvertRequest(controllerContext.HttpContext);
            ContentNegotiationResult result = contentNegotiator.Negotiate(actionReturnValue.GetType(), requestMessage, configuration.Formatters);
            if (result == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotAcceptable);
            }
            ObjectContent content = new ObjectContent(actionReturnValue, result.Formatter, result.MediaType);
            content.FormatterContext = new FormatterContext(controllerContext, actionDescriptor);
            return content;
        }

        protected virtual ControllerDescriptor CreateControllerDescriptor(Type controllerType)
        {
            return new ApiControllerDescriptor(controllerType);
        }

        protected override ControllerDescriptor GetControllerDescriptor(ControllerContext controllerContext)
        {
            return GetControllerDescriptor(controllerContext.Controller.GetType());
        }

        internal ControllerDescriptor GetControllerDescriptor(Type controllerType)
        {
            // Had to replicate DescriptorCache from Mvc source :(
            return this.DescriptorCache.GetDescriptor(controllerType, () => CreateControllerDescriptor(controllerType));
        }

        #region Static Members

        private static readonly object _invokeActionTag = new object();
        private static readonly ControllerDescriptorCache _staticDescriptorCache;

        static ApiControllerActionInvoker()
        {
            _staticDescriptorCache = new ControllerDescriptorCache();
        }

        internal static void ValidateRequest(ControllerContext controllerContext)
        {
            if (!controllerContext.IsChildAction)
            {
                ValidationUtility.EnableDynamicValidation(HttpContext.Current);
                controllerContext.HttpContext.Request.ValidateInput();
            }
        }

        private static IAsyncResult BeginInvokeAction_MakeSynchronousAsyncResult(AsyncCallback callback, object state)
        {
            SimpleAsyncResult simpleAsyncResult = new SimpleAsyncResult(state);
            simpleAsyncResult.MarkCompleted(true, callback);
            return simpleAsyncResult;
        }

        private static IAsyncResult BeginInvokeAction_ActionNotFound(AsyncCallback callback, object state)
        {
            BeginInvokeDelegate beginDelegate = new BeginInvokeDelegate(BeginInvokeAction_MakeSynchronousAsyncResult);
            EndInvokeDelegate<bool> endDelegate = (IAsyncResult asyncResult) => false;
            return AsyncResultWrapper.Begin<bool>(callback, state, beginDelegate, endDelegate, _invokeActionTag);
        }

        #endregion
    }
}
