namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Web;
    using System.Web.Mvc;
    using MvcApi.Properties;
    using System.Text;
    using System.Globalization;
    using System.Threading;
    #endregion

    public class ApiControllerActionSelector : IActionSelector
    {
        #region Nested Type

        private class ActionSelectorCacheItem
        {
            #region Fields
            private const string ActionRouteKey = "action";
            private const string ControllerRouteKey = "controller";

            private readonly ApiActionDescriptor[] _actionDescriptors;
            private readonly IDictionary<MethodInfo, string[]> _actionParameterNames;
            private readonly ApiActionDescriptor[] _aliasedMethods;
            private readonly string[] _cacheListVerbKinds;
            private readonly ApiActionDescriptor[][] _cacheListVerbs;
            private readonly ControllerDescriptor _controllerDescriptor;
            private readonly ILookup<string, ApiActionDescriptor> _nonAliasedMethods;
            #endregion

            public ActionSelectorCacheItem(ControllerDescriptor controllerDescriptor)
            {
                this._actionParameterNames = new Dictionary<MethodInfo, string[]>();
                this._cacheListVerbKinds = new string[] { HttpMethods.Get, HttpMethods.Put, HttpMethods.Post };
                this._controllerDescriptor = controllerDescriptor;
                ActionDescriptor[] array = controllerDescriptor.GetCanonicalActions();
                this._actionDescriptors = Array.ConvertAll<ActionDescriptor, ApiActionDescriptor>(array, a => a as ApiActionDescriptor);

                foreach (var action in this._actionDescriptors)
                {
                    //this._actionParameterNames.Add(action.MethodInfo, from parameter in action.MethodInfo.GetParameters()
                    //                                                  where TypeHelper.IsSimpleType(parameter.ParameterType) && !parameter.IsOptional
                    //                                                  select parameter.Name);

                    // Build action parameter name mapping, only consider parameters that are simple types, do not have default values and come from URI
                    this._actionParameterNames.Add(
                        action.MethodInfo,
                        action.MethodInfo.GetParameters()
                            .Where(binding => TypeHelper.IsSimpleType(binding.ParameterType) && !binding.IsOptional)
                            .Select(binding => binding.Name).ToArray());
                }
                this._aliasedMethods = Array.FindAll<ApiActionDescriptor>(this._actionDescriptors, new Predicate<ApiActionDescriptor>(ApiControllerActionSelector.ActionSelectorCacheItem.IsMethodDecoratedWithAliasingAttribute));
                this._nonAliasedMethods = this._actionDescriptors.Except<ApiActionDescriptor>(this._aliasedMethods).ToLookup<ApiActionDescriptor, string>(actionDesc => actionDesc.MethodInfo.Name, StringComparer.OrdinalIgnoreCase);
                this._cacheListVerbs = new ApiActionDescriptor[this._cacheListVerbKinds.Length][];
            }

            #region Properties

            public ControllerDescriptor ControllerDescriptor
            {
                get
                {
                    return this._controllerDescriptor;
                }
            }

            #endregion

            public ActionDescriptor SelectAction(ControllerContext controllerContext)
            {
                object action;
                ICollection<ApiActionDescriptor> descriptors;
                bool hasAction = controllerContext.RouteData.Values.TryGetValue("action", out action);
                string incomingMethod = controllerContext.RequestContext.GetHttpMethod();
                if (hasAction)
                {
                    string actionName = action.ToString();
                    ApiActionDescriptor[] descriptorsFound = this.GetMatchingAliasedMethods(controllerContext, actionName).Union<ApiActionDescriptor>(this._nonAliasedMethods[actionName]).ToArray<ApiActionDescriptor>();

                    if (descriptorsFound.Length == 0)
                    {
                        throw new HttpException((int)HttpStatusCode.NotFound, Error.Format(SRResources.ApiControllerActionSelector_ActionNameNotFound, new object[] { this._controllerDescriptor.ControllerName, actionName }));
                    }
                    descriptors = RemoveIncompatibleVerbs(controllerContext, incomingMethod, descriptorsFound).ToArray<ApiActionDescriptor>();
                }
                else
                {
                    descriptors = this.FindActionsForVerb(incomingMethod, controllerContext);
                }
                if (descriptors.Count == 0)
                {
                    throw new HttpException((int)HttpStatusCode.MethodNotAllowed, Error.Format(SRResources.ApiControllerActionSelector_HttpMethodNotSupported, incomingMethod));
                }
                if (descriptors.Count > 1)
                {
                    descriptors = this.FindActionUsingRouteAndQueryParameters(controllerContext, descriptors).ToArray<ApiActionDescriptor>();
                }
                if (descriptors.Count > 1)
                {
                    // if multiple matches still try to narrow by ActionResult.
                    descriptors = FilterActionResult(descriptors);
                }
                // Already done in RemoveIncompatible.
                //is2 = RunSelectionFilters(controllerContext, is2);
                switch (descriptors.Count)
                {
                    case 0:
                        throw new HttpException((int)HttpStatusCode.NotFound, Error.Format(SRResources.ApiControllerActionSelector_ActionNotFound, this._controllerDescriptor.ControllerName));
                    case 1:
                        return descriptors.First();
                }
                string message = CreateAmbiguousMatchList(descriptors);
                throw new HttpException((int)HttpStatusCode.InternalServerError, string.Format(CultureInfo.CurrentCulture, SRResources.ActionMethodSelector_AmbiguousMatch, message));
            }

            private ApiActionDescriptor[] FilterActionResult(ICollection<ApiActionDescriptor> descriptors)
            {
                return descriptors.Where(descriptor => descriptor.ReturnType != typeof(ActionResult)).ToArray();
            }

            private ApiActionDescriptor[] FindActionsForVerb(string verb, ControllerContext controllerContext)
            {
                bool supportedVerb = IsSupportedVerb(verb);
                List<ApiActionDescriptor> list = new List<ApiActionDescriptor>();
                Func<ActionMethodSelectorAttribute, bool> predicate = null;
                foreach (ApiActionDescriptor descriptor in this._actionDescriptors)
                {
                    ICollection<ActionMethodSelectorAttribute> customAttributes = descriptor.GetActionMethodSelectorAttributes();
                    if (customAttributes.Count > 0)
                    {
                        if (predicate == null)
                        {
                            predicate = attr => attr.IsValidForRequest(controllerContext, descriptor.MethodInfo);
                        }
                        if (customAttributes.All<ActionMethodSelectorAttribute>(predicate))
                        {
                            list.Add(descriptor);
                            continue;
                        }
                    }
                    if (supportedVerb && descriptor.ActionName.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(descriptor);
                    }
                }
                return list.ToArray();
            }

            private IEnumerable<ApiActionDescriptor> FindActionUsingRouteAndQueryParameters(ControllerContext controllerContext, IEnumerable<ApiActionDescriptor> actionsFound)
            {
                // TODO improve performance of this method.
                IDictionary<string, object> routeValues = controllerContext.RouteData.Values;
                IEnumerable<string> routeParameterNames = routeValues.Select(route => route.Key)
                    .Where(key =>
                           !String.Equals(key, ControllerRouteKey, StringComparison.OrdinalIgnoreCase) &&
                           !String.Equals(key, ActionRouteKey, StringComparison.OrdinalIgnoreCase));

                IEnumerable<string> queryParameterNames = controllerContext.RequestContext.QueryString().AllKeys; ;
                bool hasRouteParameters = routeParameterNames.Any();
                bool hasQueryParameters = queryParameterNames.Any();

                if (hasRouteParameters || hasQueryParameters)
                {
                    //// refine the results based on route parameters to make sure that route parameters take precedence over query parameters
                    //if (hasRouteParameters && hasQueryParameters)
                    //{
                    //    // route parameters is a subset of action parameters
                    //    actionsFound = actionsFound.Where(descriptor => !routeParameterNames.Except(_actionParameterNames[descriptor.MethodInfo], StringComparer.OrdinalIgnoreCase).Any());
                    //}

                    // further refine the results making sure that action parameters is a subset of route parameters and query parameters
                    if (actionsFound.Count() > 1)
                    {
                        IEnumerable<string> combinedParameterNames = queryParameterNames.Union(routeParameterNames);

                        // action parameters is a subset of route parameters and query parameters
                        actionsFound = actionsFound.Where(descriptor => !_actionParameterNames[descriptor.MethodInfo].Except(combinedParameterNames, StringComparer.OrdinalIgnoreCase).Any());

                        // select the results with the longest parameter match 
                        if (actionsFound.Count() > 1)
                        {
                            actionsFound = actionsFound
                                .GroupBy(descriptor => _actionParameterNames[descriptor.MethodInfo].Count())
                                .OrderByDescending(g => g.Key)
                                .First();
                        }
                    }
                }
                else
                {
                    // return actions with no parameters
                    actionsFound = actionsFound.Where(descriptor => _actionParameterNames[descriptor.MethodInfo].Length == 0);
                }

                return actionsFound;
            }

            private IEnumerable<ApiActionDescriptor> GetMatchingAliasedMethods(ControllerContext controllerContext, string actionName)
            {
                return (from descriptor in this._aliasedMethods
                        where descriptor.GetActionNameSelectorAttributes()
                        .All<ActionNameSelectorAttribute>(attr => attr.IsValidName(controllerContext, actionName, descriptor.MethodInfo))
                        select descriptor);
            }

            #region Static Methods

            private static string CreateAmbiguousMatchList(IEnumerable<ApiActionDescriptor> ambiguousDescriptors)
            {
                StringBuilder builder = new StringBuilder();
                foreach (ApiActionDescriptor descriptor in ambiguousDescriptors)
                {
                    MethodInfo methodInfo = descriptor.MethodInfo;
                    builder.AppendLine();
                    builder.Append(Error.Format(SRResources.ActionMethodSelector_AmbiguousMatchType, methodInfo, methodInfo.DeclaringType.FullName));
                }
                return builder.ToString();
            }

            private static bool IsMethodDecoratedWithAliasingAttribute(ReflectedActionDescriptor actionDesc)
            {
                bool inherit = true;
                return actionDesc.MethodInfo.IsDefined(ApiControllerActionSelector.ActionNameSelectorType, inherit);
            }

            private static bool IsSupportedVerb(string verb)
            {
                return (((verb == HttpMethods.Get) || (verb == HttpMethods.Post)) || (verb == HttpMethods.Put)) || (verb == HttpMethods.Delete);
            }

            private static bool IsValidActionMethod(MethodInfo methodInfo)
            {
                if (methodInfo.IsSpecialName)
                {
                    return false;
                }
                if (methodInfo.GetBaseDefinition().DeclaringType.IsAssignableFrom(TypeHelper.ApiControllerType))
                {
                    return false;
                }
                return true;
            }

            private static IEnumerable<ApiActionDescriptor> RemoveIncompatibleVerbs(ControllerContext controllerContext, string incomingMethod, IEnumerable<ApiActionDescriptor> descriptorsFound)
            {
                return descriptorsFound.Where(actionDescriptor =>
                {
                    ICollection<ActionMethodSelectorAttribute> source = actionDescriptor.GetActionMethodSelectorAttributes();
                    // Action name can not a verb that does not match incoming verb
                    if (source.Count > 0)
                    {
                        return source.All(attr => attr.IsValidForRequest(controllerContext, actionDescriptor.MethodInfo));
                    }
                    return IsValid(incomingMethod, actionDescriptor.ActionName);
                });
            }

            private static bool IsValid(string incomingMethod, string actionName)
            {
                return HttpMethods.AllowedVerbs.Any(verb => verb.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                            && actionName.Equals(incomingMethod, StringComparison.OrdinalIgnoreCase);
            }

            private static List<ApiActionDescriptor> RunSelectionFilters(ControllerContext controllerContext, IEnumerable<ApiActionDescriptor> descriptors)
            {
                // remove all methods which are opting out of this request
                // to opt out, at least one attribute defined on the method must return false

                List<ApiActionDescriptor> matchesWithSelectionAttributes = new List<ApiActionDescriptor>();
                List<ApiActionDescriptor> matchesWithoutSelectionAttributes = new List<ApiActionDescriptor>();

                foreach (ApiActionDescriptor actionDescriptor in descriptors)
                {
                    ICollection<ActionMethodSelectorAttribute> attrs = ReflectedAttributeCache.GetActionMethodSelectorAttributes(actionDescriptor.MethodInfo);
                    if (attrs.Count == 0)
                    {
                        matchesWithoutSelectionAttributes.Add(actionDescriptor);
                    }
                    else if (attrs.All(attr => attr.IsValidForRequest(controllerContext, actionDescriptor.MethodInfo)))
                    {
                        matchesWithSelectionAttributes.Add(actionDescriptor);
                    }
                }

                // if a matching action method had a selection attribute, consider it more specific than a matching action method
                // without a selection attribute
                return (matchesWithSelectionAttributes.Count > 0) ? matchesWithSelectionAttributes : matchesWithoutSelectionAttributes;
            }

            #endregion
        }
        #endregion

        private object _cacheKey;
        private ActionSelectorCacheItem cachedActionSelector;

        public ApiControllerActionSelector()
        {
            this._cacheKey = new object();
        }

        public virtual ILookup<string, ApiActionDescriptor> GetActionMapping(ControllerDescriptor controllerDescriptor)
        {
            if (controllerDescriptor == null)
            {
                throw Error.ArgumentNull("controllerDescriptor");
            }
            //return this.GetInternalSelector(controllerDescriptor).GetActionMapping();
            throw new NotImplementedException();
        }

        public virtual ActionDescriptor SelectAction(ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw Error.ArgumentNull("controllerContext");
            }
            var controller = controllerContext.Controller as Controller;
            if (controller == null)
            {
                throw new InvalidOperationException("Controller must inherit from System.Web.Mvc.Controller");
            }
            ApiControllerActionInvoker invoker = controller.ActionInvoker as ApiControllerActionInvoker;
            if (invoker == null)
            {
                throw new InvalidOperationException("ActionInvoker must inherit from ApiControllerActionInvoker.");
            }

            var controllerDescriptor = invoker.GetControllerDescriptor(controller.GetType());
            Func<object, object> valueFactory = null;

            if (this.cachedActionSelector == null)
            {
                ActionSelectorCacheItem actionSelectorItem = new ActionSelectorCacheItem(controllerDescriptor);
                Interlocked.CompareExchange<ActionSelectorCacheItem>(ref this.cachedActionSelector, actionSelectorItem, null);
                return actionSelectorItem.SelectAction(controllerContext);
            }
            if (this.cachedActionSelector.ControllerDescriptor == controllerDescriptor)
            {
                return this.cachedActionSelector.SelectAction(controllerContext);
            }
            if (valueFactory == null)
            {
                valueFactory = _ => new ActionSelectorCacheItem(controllerDescriptor);
            }
            var properties = ((ApiControllerDescriptor)controllerDescriptor).Properties;
            ActionSelectorCacheItem orAdd = (ActionSelectorCacheItem)properties.GetOrAdd(this._cacheKey, valueFactory);
            return orAdd.SelectAction(controllerContext);
        }

        //private ActionSelectorCacheItem GetInternalSelector(ControllerDescriptor controllerDescriptor)
        //{
        //    if (this._fastCache == null)
        //    {
        //        ActionSelectorCacheItem selector = new ActionSelectorCacheItem(controllerDescriptor);
        //        Interlocked.CompareExchange<ActionSelectorCacheItem>(ref this._fastCache, selector, null);
        //        return selector;
        //    }
        //    if (this._fastCache.HttpControllerDescriptor == controllerDescriptor)
        //    {
        //        return this._fastCache;
        //    }
        //    return (ActionSelectorCacheItem)controllerDescriptor.Properties.GetOrAdd(this._cacheKey, _ => new ActionSelectorCacheItem(controllerDescriptor));
        //}

        private static readonly Type ActionNameSelectorType = typeof(ActionNameSelectorAttribute);

    }
}
