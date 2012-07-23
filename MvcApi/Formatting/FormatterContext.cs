namespace MvcApi.Formatting
{
    using System.Net.Http.Headers;
    using System.Web.Mvc;
    using MvcApi.Http;

    public sealed class FormatterContext : ControllerContext
    {
        public FormatterContext(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
            : base(controllerContext)
        {
            this.ActionDescriptor = actionDescriptor;
            this.Request = HttpExtensions.ConvertRequest(controllerContext.HttpContext);
            this.Response = new HttpResponseMessage(controllerContext.HttpContext);
            this.Response.RequestMessage = this.Request;
        }

        public ActionDescriptor ActionDescriptor { get; set; }

        public MediaTypeHeaderValue ContentType { get; private set; }

        public HttpRequestMessage Request { get; private set; }

        public HttpResponseMessage Response { get; private set; }
    }
}
