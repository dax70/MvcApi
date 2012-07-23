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
    #endregion

    public class ApiControllerActionSelector_old
    {
        private class ActionSelectorCacheItem
        {
            private readonly ApiActionDescriptor[] _actionDescriptors;
            private readonly IDictionary<MethodInfo, IEnumerable<string>> _actionParameterNames;
            private readonly ApiActionDescriptor[] _aliasedMethods;
            private readonly string[] _cacheListVerbKinds;
            private readonly ApiActionDescriptor[][] _cacheListVerbs;
            private readonly ControllerDescriptor _controllerDescriptor;
            private readonly ILookup<string, ApiActionDescriptor> _nonAliasedMethods;

            private bool cacheListVerbsInited = false;

            public ActionSelectorCacheItem(ControllerDescriptor controllerDescriptor)
            {
                this._actionParameterNames = new Dictionary<MethodInfo, IEnumerable<string>>();
                this._cacheListVerbKinds = new string[] { HttpMethods.Get, HttpMethods.Put, HttpMethods.Post };
                this._controllerDescriptor = controllerDescriptor;
                ActionDescriptor[] array = controllerDescriptor.GetCanonicalActions();
                this._actionDescriptors = Array.ConvertAll<ActionDescriptor, ApiActionDescriptor>(array, a => a as ApiActionDescriptor);


                var methodInfos = this._actionDescriptors.Select(a => a.MethodInfo);
                foreach (MethodInfo info in methodInfos)
                {
                    this._actionParameterNames.Add(info, from parameter in info.GetParameters()
                                                         where TypeHelper.IsSimpleType(parameter.ParameterType) && !parameter.IsOptional
                                                         select parameter.Name);
                }
                this._aliasedMethods = Array.FindAll<ApiActionDescriptor>(this._actionDescriptors, new Predicate<ApiActionDescriptor>(ApiControllerActionSelector_old.ActionSelectorCacheItem.IsMethodDecoratedWithAliasingAttribute));
                this._nonAliasedMethods = this._actionDescriptors.Except<ApiActionDescriptor>(this._aliasedMethods).ToLookup<ApiActionDescriptor, string>(actionDesc => actionDesc.MethodInfo.Name, StringComparer.OrdinalIgnoreCase);
                this._cacheListVerbs = new ApiActionDescriptor[this._cacheListVerbKinds.Length][];
            }

            public ActionDescriptor SelectAction(ControllerContext controllerContext)
            {
                if (!cacheListVerbsInited)
                {
                    int length = this._cacheListVerbKinds.Length;
                    for (int i = 0; i < length; i++)
                    {
                        this._cacheListVerbs[i] = this.FindActionsForVerbWorker(this._cacheListVerbKinds[i], controllerContext);
                    }
                    cacheListVerbsInited = true;
                }
                object action;
                ICollection<ApiActionDescriptor> is2;
                bool flag = controllerContext.RouteData.Values.TryGetValue("action", out action);
                string incomingMethod = controllerContext.RequestContext.GetHttpMethod();
                if (flag)
                {
                    string actionName = action.ToString();
                    ApiActionDescriptor[] descriptorsFound = this.GetMatchingAliasedMethods(controllerContext, actionName).Union<ApiActionDescriptor>(this._nonAliasedMethods[actionName]).ToArray<ApiActionDescriptor>();
                    if (descriptorsFound.Length == 0)
                    {
                        throw new HttpException((int)HttpStatusCode.NotFound, Error.Format(SRResources.ApiControllerActionSelector_ActionNameNotFound, new object[] { this._controllerDescriptor.ControllerName, actionName }));
                    }
                    is2 = RemoveIncompatibleVerbs(controllerContext, incomingMethod, descriptorsFound).ToArray<ApiActionDescriptor>();
                }
                else
                {
                    is2 = this.FindActionsForVerb(incomingMethod, controllerContext);
                }
                if (is2.Count == 0)
                {
                    throw new HttpException((int)HttpStatusCode.MethodNotAllowed, Error.Format(SRResources.ApiControllerActionSelector_HttpMethodNotSupported, incomingMethod));
                }
                if (is2.Count > 1)
                {
                    is2 = this.FindActionUsingRouteAndQueryParameters(controllerContext, is2).ToArray<ApiActionDescriptor>();
                }
                //List<ApiActionDescriptor> ambiguousDescriptors = RunSelectionFilters(controllerContext, is2);
                //is2 = null;

                return null;
            }

            private ApiActionDescriptor[] FindActionsForVerb(string verb, ControllerContext controllerContext)
            {
                for (int i = 0; i < this._cacheListVerbKinds.Length; i++)
                {
                    if (verb == this._cacheListVerbKinds[i])
                    {
                        return this._cacheListVerbs[i];
                    }
                }
                return this.FindActionsForVerbWorker(verb, controllerContext);
            }

            private ApiActionDescriptor[] FindActionsForVerbWorker(string verb, ControllerContext controllerContext)
            {
                bool flag = (((verb == HttpMethods.Get) || (verb == HttpMethods.Post)) || (verb == HttpMethods.Put)) || (verb == HttpMethods.Delete);
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
                    if (flag && descriptor.ActionName.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(descriptor);
                    }
                }
                return list.ToArray();
            }
            private IEnumerable<ApiActionDescriptor> FindActionUsingRouteAndQueryParameters(ControllerContext controllerContext, IEnumerable<ApiActionDescriptor> actionsFound)
            {
                Func<ApiActionDescriptor, bool> predicate = null;
                Func<ApiActionDescriptor, int> keySelector = null;
                Func<ApiActionDescriptor, bool> func3 = null;
                IDictionary<string, object> values = controllerContext.RouteData.Values;
                IEnumerable<string> routeParameterNames = from route in values
                                                          select route.Key into key
                                                          where !string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase) && !string.Equals(key, "action", StringComparison.OrdinalIgnoreCase)
                                                          select key;
                IEnumerable<string> allKeys = controllerContext.RequestContext.QueryString().AllKeys;
                bool flag = routeParameterNames.Any<string>();
                bool flag2 = allKeys.Any<string>();
                if (flag || flag2)
                {
                    if (flag && flag2)
                    {
                        if (predicate == null)
                        {
                            predicate = descriptor => !routeParameterNames.Except<string>(this._actionParameterNames[descriptor.MethodInfo], StringComparer.OrdinalIgnoreCase).Any<string>();
                        }
                        actionsFound = actionsFound.Where<ApiActionDescriptor>(predicate);
                    }
                    if (actionsFound.Count<ApiActionDescriptor>() > 1)
                    {
                        IEnumerable<string> combinedParameterNames = allKeys.Union<string>(routeParameterNames);
                        actionsFound = from descriptor in actionsFound
                                       where !this._actionParameterNames[descriptor.MethodInfo].Except<string>(combinedParameterNames, StringComparer.OrdinalIgnoreCase).Any<string>()
                                       select descriptor;
                        if (actionsFound.Count<ApiActionDescriptor>() <= 1)
                        {
                            return actionsFound;
                        }
                        if (keySelector == null)
                        {
                            keySelector = descriptor => this._actionParameterNames[descriptor.MethodInfo].Count<string>();
                        }
                        actionsFound = (from g in actionsFound.GroupBy<ApiActionDescriptor, int>(keySelector)
                                        orderby g.Key descending
                                        select g).First<IGrouping<int, ApiActionDescriptor>>();
                    }
                    return actionsFound;
                }
                if (func3 == null)
                {
                    func3 = descriptor => !this._actionParameterNames[descriptor.MethodInfo].Any<string>();
                }
                actionsFound = actionsFound.Where<ApiActionDescriptor>(func3);
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

            private static bool IsMethodDecoratedWithAliasingAttribute(ReflectedActionDescriptor actionDesc)
            {
                bool inherit = true;
                return actionDesc.MethodInfo.IsDefined(ApiControllerActionSelector_old.ActionNameSelectorType, inherit);
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
                return descriptorsFound.Where<ApiActionDescriptor>(actionDescriptor =>
                {
                    Func<ActionMethodSelectorAttribute, bool> predicate = null;
                    MethodInfo method = actionDescriptor.MethodInfo;
                    ICollection<ActionMethodSelectorAttribute> source = actionDescriptor.GetActionMethodSelectorAttributes();
                    if (source.Count <= 0)
                    {
                        return true;
                    }
                    if (predicate == null)
                    {
                        predicate = attr => attr.IsValidForRequest(controllerContext, method);
                    }
                    return source.All<ActionMethodSelectorAttribute>(predicate);
                });
            }

            private static List<MethodInfo> RunSelectionFilters(ControllerContext controllerContext, List<MethodInfo> methodInfos)
            {
                List<MethodInfo> list = new List<MethodInfo>();
                List<MethodInfo> list2 = new List<MethodInfo>();
                using (List<MethodInfo>.Enumerator enumerator = methodInfos.GetEnumerator())
                {
                    Func<ActionMethodSelectorAttribute, bool> predicate = null;
                    MethodInfo methodInfo;
                    while (enumerator.MoveNext())
                    {
                        methodInfo = enumerator.Current;
                        ICollection<ActionMethodSelectorAttribute> actionMethodSelectorAttributes = ReflectedAttributeCache.GetActionMethodSelectorAttributes(methodInfo);
                        if (actionMethodSelectorAttributes.Count == 0)
                        {
                            list2.Add(methodInfo);
                        }
                        else
                        {
                            if (predicate == null)
                            {
                                predicate = delegate(ActionMethodSelectorAttribute attr)
                                {
                                    return attr.IsValidForRequest(controllerContext, methodInfo);
                                };
                            }
                            if (actionMethodSelectorAttributes.All<ActionMethodSelectorAttribute>(predicate))
                            {
                                list.Add(methodInfo);
                            }
                        }
                    }
                }
                if (list.Count <= 0)
                {
                    return list2;
                }
                return list;
            }

            #endregion
        }

        public ApiControllerActionSelector_old()
        {
        }

        public virtual ActionDescriptor SelectAction(Controller controller)
        {
            ApiControllerActionInvoker invoker = controller.ActionInvoker as ApiControllerActionInvoker;
            if (invoker == null)
            {
                throw new InvalidOperationException("ActionInvoker must inherit from ApiControllerActionInvoker.");
            }

            var controllerContext = controller.ControllerContext;


            return null;
        }

        private static readonly Type ActionNameSelectorType = typeof(ActionNameSelectorAttribute);
    }
}
