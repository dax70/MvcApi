namespace MvcApi//System.Web.Mvc.Async
{
    #region Using Directives
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Web.Mvc;
    using System.Web.Mvc.Async;
    using MvcApi; 
    #endregion

    internal static class AsyncResultWrapper
    {
        #region Nested Type

        [DebuggerNonUserCode, SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "The Timer will be disposed of either when it fires or when the operation completes successfully.")]
        private sealed class WrappedAsyncResult<TResult> : IAsyncResult
        {
            private readonly BeginInvokeDelegate _beginDelegate;
            private readonly object _beginDelegateLockObj;
            private readonly EndInvokeDelegate<TResult> _endDelegate;
            private readonly SingleEntryGate _endExecutedGate;
            private readonly SingleEntryGate _handleCallbackGate;
            private IAsyncResult _innerAsyncResult;
            private AsyncCallback _originalCallback;
            private readonly object _tag;
            private volatile bool _timedOut;
            private Timer _timer;

            public WrappedAsyncResult(BeginInvokeDelegate beginDelegate, EndInvokeDelegate<TResult> endDelegate, object tag)
            {
                this._beginDelegateLockObj = new object();
                this._endExecutedGate = new SingleEntryGate();
                this._handleCallbackGate = new SingleEntryGate();
                this._beginDelegate = beginDelegate;
                this._endDelegate = endDelegate;
                this._tag = tag;
            }

            public void Begin(AsyncCallback callback, object state, int timeout)
            {
                bool completedSynchronously;
                this._originalCallback = callback;
                lock (this._beginDelegateLockObj)
                {
                    this._innerAsyncResult = this._beginDelegate(new AsyncCallback(this.HandleAsynchronousCompletion), state);
                    completedSynchronously = this._innerAsyncResult.CompletedSynchronously;
                    if (!completedSynchronously && (timeout > -1))
                    {
                        this.CreateTimer(timeout);
                    }
                }
                if (completedSynchronously && (callback != null))
                {
                    callback(this);
                }
            }

            public static AsyncResultWrapper.WrappedAsyncResult<TResult> Cast(IAsyncResult asyncResult, object tag)
            {
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }
                AsyncResultWrapper.WrappedAsyncResult<TResult> result = asyncResult as AsyncResultWrapper.WrappedAsyncResult<TResult>;
                if ((result == null) || !object.Equals(result._tag, tag))
                {
                    throw Error.AsyncCommon_InvalidAsyncResult("asyncResult");
                }
                return result;
            }

            private void CreateTimer(int timeout)
            {
                this._timer = new Timer(new TimerCallback(this.HandleTimeout), null, timeout, -1);
            }

            public TResult End()
            {
                if (!this._endExecutedGate.TryEnter())
                {
                    throw Error.AsyncCommon_AsyncResultAlreadyConsumed();
                }
                if (this._timedOut)
                {
                    throw new TimeoutException();
                }
                this.WaitForBeginToCompleteAndDestroyTimer();
                return this._endDelegate(this._innerAsyncResult);
            }

            private void ExecuteAsynchronousCallback(bool timedOut)
            {
                this.WaitForBeginToCompleteAndDestroyTimer();
                if (this._handleCallbackGate.TryEnter())
                {
                    this._timedOut = timedOut;
                    if (this._originalCallback != null)
                    {
                        this._originalCallback(this);
                    }
                }
            }

            private void HandleAsynchronousCompletion(IAsyncResult asyncResult)
            {
                if (!asyncResult.CompletedSynchronously)
                {
                    this.ExecuteAsynchronousCallback(false);
                }
            }

            private void HandleTimeout(object state)
            {
                this.ExecuteAsynchronousCallback(true);
            }

            private void WaitForBeginToCompleteAndDestroyTimer()
            {
                lock (this._beginDelegateLockObj)
                {
                    if (this._timer != null)
                    {
                        this._timer.Dispose();
                    }
                    this._timer = null;
                }
            }

            public object AsyncState
            {
                get
                {
                    return this._innerAsyncResult.AsyncState;
                }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    return this._innerAsyncResult.AsyncWaitHandle;
                }
            }

            public bool CompletedSynchronously
            {
                get
                {
                    return this._innerAsyncResult.CompletedSynchronously;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    return this._innerAsyncResult.IsCompleted;
                }
            }
        }

        #endregion

        public static IAsyncResult Begin(AsyncCallback callback, object state, BeginInvokeDelegate beginDelegate, EndInvokeDelegate endDelegate, object tag)
        {
            return Begin(callback, state, beginDelegate, endDelegate, tag, -1);
        }

        public static IAsyncResult Begin(AsyncCallback callback, object state, BeginInvokeDelegate beginDelegate, EndInvokeDelegate endDelegate, object tag, int timeout)
        {
            return Begin<AsyncVoid>(callback, state, beginDelegate, MakeVoidDelegate(endDelegate), tag, timeout);
        }

        public static IAsyncResult Begin<TResult>(AsyncCallback callback, object state, BeginInvokeDelegate beginDelegate, EndInvokeDelegate<TResult> endDelegate, object tag)
        {
            return Begin<TResult>(callback, state, beginDelegate, endDelegate, tag, -1);
        }

        public static IAsyncResult Begin<TResult>(AsyncCallback callback, object state, BeginInvokeDelegate beginDelegate, EndInvokeDelegate<TResult> endDelegate, object tag, int timeout)
        {
            WrappedAsyncResult<TResult> result = new WrappedAsyncResult<TResult>(beginDelegate, endDelegate, tag);
            result.Begin(callback, state, timeout);
            return result;
        }

        public static IAsyncResult BeginSynchronous(AsyncCallback callback, object state, Action action, object tag)
        {
            return BeginSynchronous<AsyncVoid>(callback, state, MakeVoidDelegate(action), tag);
        }

        public static IAsyncResult BeginSynchronous<TResult>(AsyncCallback callback, object state, Func<TResult> func, object tag)
        {
            BeginInvokeDelegate beginDelegate = delegate(AsyncCallback asyncCallback, object asyncState)
            {
                SimpleAsyncResult simpleAsyncResult = new SimpleAsyncResult(asyncState);
                simpleAsyncResult.MarkCompleted(true, asyncCallback);
                return simpleAsyncResult;
            }
            ;
            EndInvokeDelegate<TResult> endDelegate = (IAsyncResult _) => func();
            AsyncResultWrapper.WrappedAsyncResult<TResult> wrappedAsyncResult = new AsyncResultWrapper.WrappedAsyncResult<TResult>(beginDelegate, endDelegate, tag);
            wrappedAsyncResult.Begin(callback, state, -1);
            return wrappedAsyncResult;
        }

        public static void End(IAsyncResult asyncResult, object tag)
        {
            End<AsyncVoid>(asyncResult, tag);
        }

        public static TResult End<TResult>(IAsyncResult asyncResult, object tag)
        {
            return WrappedAsyncResult<TResult>.Cast(asyncResult, tag).End();
        }

        private static Func<AsyncVoid> MakeVoidDelegate(Action action)
        {
            return delegate
            {
                action();
                return new AsyncVoid();
            };
        }

        private static EndInvokeDelegate<AsyncVoid> MakeVoidDelegate(EndInvokeDelegate endDelegate)
        {
            return delegate(IAsyncResult ar)
            {
                endDelegate(ar);
                return new AsyncVoid();
            };
        }

    }
}
