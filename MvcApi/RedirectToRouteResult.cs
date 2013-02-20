//----------------------------------------------------------------
// Original from Mvc Rest Futures
//----------------------------------------------------------------
namespace MvcApi
{
    using System.Net;
    using System.Web.Mvc;

    /// <summary>
    /// Augments the RedirectToRouteResult behavior by sending Created HTTP status code in responses to POST, OK HTTP status code otherwise
    /// </summary>
    internal class ResourceRedirectToRouteResult : ActionResult
    {
        RedirectToRouteResult inner;

        public ResourceRedirectToRouteResult(RedirectToRouteResult inner)
        {
            this.inner = inner;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            // call the base which we expect to be setting the Location header
            this.inner.ExecuteResult(context);

            if (!context.RequestContext.IsBrowserRequest())
            {
                // on POST we return Created, otherwise (EG: DELETE) we return OK
                context.HttpContext.Response.ClearContent();
                context.HttpContext.Response.StatusCode = (context.RequestContext.GetHttpMethod() == "POST") ? (int)HttpStatusCode.Created : (int)HttpStatusCode.OK;
            }
        }
    }
}

