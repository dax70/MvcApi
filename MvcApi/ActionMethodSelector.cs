namespace MvcApi//System.Web.Mvc
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Globalization;
    using System.Text;
    using System.Web.Mvc; 
    using MvcApi.Properties;
    #endregion

    internal sealed class ActionMethodSelector
    {
        public ActionMethodSelector(Type controllerType)
        {
            this.ControllerType = controllerType;
            this.PopulateLookupTables();
        }

        public Type ControllerType { get; set; }

        public MethodInfo[] AliasedMethods { get; private set; }

        public ILookup<string, MethodInfo> NonAliasedMethods { get; private set; }

        public MethodInfo FindActionMethod(ControllerContext controllerContext, string actionName)
        {
            List<MethodInfo> matchingAliasedMethods = this.GetMatchingAliasedMethods(controllerContext, actionName);
            matchingAliasedMethods.AddRange(this.NonAliasedMethods[actionName]);
            List<MethodInfo> ambiguousMethods = RunSelectionFilters(controllerContext, matchingAliasedMethods);
            switch (ambiguousMethods.Count)
            {
                case 0:
                    return null;

                case 1:
                    return ambiguousMethods[0];
            }
            throw this.CreateAmbiguousMatchException(ambiguousMethods, actionName);
        }

        internal List<MethodInfo> GetMatchingAliasedMethods(ControllerContext controllerContext, string actionName)
        {
            return 
            (from methods in
                (from methodInfo in this.AliasedMethods
                    select new
                    {
                        methodInfo = methodInfo,
                        attrs = ReflectedAttributeCache.GetActionNameSelectorAttributes(methodInfo)
                    }
                )
                .Where(a =>
                {
                    return a.attrs.All<ActionNameSelectorAttribute>(attr => 
                        attr.IsValidName(controllerContext, actionName, a.methodInfo));
                })
                select methods.methodInfo).ToList<MethodInfo>();
        }


        private void PopulateLookupTables()
        {
            MethodInfo[] array = Array.FindAll<MethodInfo>(this.ControllerType.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance), new Predicate<MethodInfo>(ActionMethodSelector.IsValidActionMethod));
            this.AliasedMethods = Array.FindAll<MethodInfo>(array, new Predicate<MethodInfo>(ActionMethodSelector.IsMethodDecoratedWithAliasingAttribute));
            this.NonAliasedMethods = array.Except<MethodInfo>(this.AliasedMethods).ToLookup<MethodInfo, string>(delegate(MethodInfo method)
            {
                return method.Name;
            }, StringComparer.OrdinalIgnoreCase);
        }

        private AmbiguousMatchException CreateAmbiguousMatchException(List<MethodInfo> ambiguousMethods, string actionName)
        {
            StringBuilder builder = new StringBuilder();
            foreach (MethodInfo info in ambiguousMethods)
            {
                string str = Convert.ToString(info, CultureInfo.CurrentCulture);
                string fullName = info.DeclaringType.FullName;
                builder.AppendLine();
                builder.AppendFormat(CultureInfo.CurrentCulture, SRResources.ActionMethodSelector_AmbiguousMatchType, new object[] { str, fullName });
            }
            return new AmbiguousMatchException(string.Format(CultureInfo.CurrentCulture, SRResources.ActionMethodSelector_AmbiguousMatch, new object[] { actionName, this.ControllerType.Name, builder }));
        }

        private static bool IsMethodDecoratedWithAliasingAttribute(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(ActionNameSelectorAttribute), true);
        }

        private static bool IsValidActionMethod(MethodInfo methodInfo)
        {
            return (!methodInfo.IsSpecialName && !methodInfo.GetBaseDefinition().DeclaringType.IsAssignableFrom(typeof(Controller)));
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

    }
}
