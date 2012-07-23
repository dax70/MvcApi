namespace MvcApi.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using MvcApi.Http;

    internal sealed class VoidHttpMessageConverter : ActionResponseConverter
    {
        public VoidHttpMessageConverter()
        {
        }

        public override HttpResponseMessage Convert(ControllerContext controllerContext, object responseValue)
        {
            throw new NotImplementedException();
        }
    }
}
