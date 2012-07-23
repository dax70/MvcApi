namespace MvcApi//System.Web.Mvc.Async
{
    using System;

    internal delegate IAsyncResult BeginInvokeDelegate(AsyncCallback callback, object state);

    internal delegate void EndInvokeDelegate(IAsyncResult asyncResult);

    internal delegate TResult EndInvokeDelegate<TResult>(IAsyncResult asyncResult);
}
