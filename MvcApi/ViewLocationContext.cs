using System;
using System.Web.Mvc;
using System.Net.Http;

namespace MvcApi
{
    public sealed class ViewLocationContext: ControllerContext
    {
        public ViewLocationContext(ControllerContext controllerContext)
            :base(controllerContext)
        {
        }

        public ActionDescriptor ActionDescriptor { get; set; }

        public Type ObjectType { get; set; }

        public object ReturnValue { get; set; }
    }
}
