namespace MvcApi
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;

    internal static class ReflectedActionDescriptoExtensions
    {
        public static Type ReturnType(this ReflectedActionDescriptor actionDescriptor)
        {
            return actionDescriptor.MethodInfo.ReturnType;
        }

        public static ICollection<ActionMethodSelectorAttribute> GetActionMethodSelectorAttributes(this ReflectedActionDescriptor actionDescriptor)
        {
            return ReflectedAttributeCache.GetActionMethodSelectorAttributes(actionDescriptor.MethodInfo);
        }

        public static ICollection<ActionNameSelectorAttribute> GetActionNameSelectorAttributes(this ReflectedActionDescriptor actionDescriptor)
        {
            return ReflectedAttributeCache.GetActionNameSelectorAttributes(actionDescriptor.MethodInfo);
        }
    }
}
