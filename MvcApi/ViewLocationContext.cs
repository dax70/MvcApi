using System;
using System.Web.Mvc;

namespace MvcApi
{
    public sealed class ViewLocationContext: ControllerContext
    {
        public ViewLocationContext(ControllerContext controllerContext)
            :base(controllerContext)
        {
        }

        public ActionDescriptor ActionDescriptor { get; set; }

        public Type ReturnType { get; set; }

        public object ReturnValue { get; set; }
    }
}
