namespace MvcApi
{
    using System;
    using System.Web.Mvc;

    public interface IApiAsyncActionInvoker
    {
        IAsyncResult BeginInvokeActionDescriptor(ControllerContext controllerContext, ActionDescriptor actionDescriptor, AsyncCallback callback, object state);
        
        bool EndInvokeActionDescriptor(IAsyncResult asyncResult);
    }
}
