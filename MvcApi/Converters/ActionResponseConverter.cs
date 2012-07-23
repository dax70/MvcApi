namespace MvcApi.Converters
{
    using System;
    using System.Web.Mvc;
    using MvcApi.Http;

    internal abstract class ActionResponseConverter
    {
        protected ActionResponseConverter()
        {
        }

        public abstract HttpResponseMessage Convert(ControllerContext controllerContext, object responseValue);

    }
}
