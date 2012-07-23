namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Web.Mvc;
    #endregion

    public class ApiActionDescriptor : ReflectedActionDescriptor
    {
        public ApiActionDescriptor(MethodInfo methodInfo, string actionName, ControllerDescriptor controllerDescriptor)
            : base(methodInfo, actionName, controllerDescriptor)
        {
        }

        public Type ReturnType
        {
            get
            {
                var methodInfo = this.MethodInfo;
                if (methodInfo != null)
                {
                    return methodInfo.ReturnType;
                }
                return null;
            }
        }

        public ICollection<ActionMethodSelectorAttribute> GetActionMethodSelectorAttributes()
        {
            return ReflectedAttributeCache.GetActionMethodSelectorAttributes(this.MethodInfo);
        }

        public ICollection<ActionNameSelectorAttribute> GetActionNameSelectorAttributes()
        {
            return ReflectedAttributeCache.GetActionNameSelectorAttributes(this.MethodInfo);
        }
    }
}
