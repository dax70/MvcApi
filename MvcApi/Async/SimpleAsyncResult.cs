namespace MvcApi//System.Web.Mvc.Async
{
    using System;
    using System.Threading;

    internal sealed class SimpleAsyncResult : IAsyncResult
    {
        private readonly object _asyncState;
        private bool _completedSynchronously;
        private volatile bool _isCompleted;

        public SimpleAsyncResult(object asyncState)
        {
            this._asyncState = asyncState;
        }

        public void MarkCompleted(bool completedSynchronously, AsyncCallback callback)
        {
            this._completedSynchronously = completedSynchronously;
            this._isCompleted = true;
            if (callback != null)
            {
                callback(this);
            }
        }

        public object AsyncState
        {
            get { return this._asyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return null; }
        }

        public bool CompletedSynchronously
        {
            get { return this._completedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return this._isCompleted; }
        }
    }



}
