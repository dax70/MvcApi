namespace MvcApi.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using MvcApi.Http;

    internal sealed class HttpMessageConverter: ActionResponseConverter
    {
        public HttpMessageConverter()
        {
        }

        public override HttpResponseMessage Convert(ControllerContext controllerContext, object responseValue)
        {
            throw new NotImplementedException();
        }
    }
}
