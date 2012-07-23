namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Web;
    using System.Web.Mvc;
    #endregion

    public class ApiController : Controller
    {
        private ApiConfiguration configuration;

        public ApiController()
        {
        }

        protected ApiConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    // TODO: perhaps get from ControllerContext (ext method).
                    configuration = GlobalConfiguration.Configuration;
                }
                return configuration;
            }
        }

        protected override IActionInvoker CreateActionInvoker()
        {
            return this.Configuration.Services.GetActionInvoker();
        }

        protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
        {
            this.PossiblyLoadTempData();
            IAsyncResult result = null;
            try
            {
                var controllerContext = this.ControllerContext;
                ServicesContainer controllerServices = this.Configuration.Services;

                ApiControllerActionInvoker asyncInvoker = this.ActionInvoker as ApiControllerActionInvoker;
                ActionDescriptor actionDescriptor = controllerServices.GetActionSelector().SelectAction(controllerContext);
                if (asyncInvoker != null)
                {
                    BeginInvokeDelegate beginDelegate = (AsyncCallback asyncCallback, object asyncState) => asyncInvoker.BeginInvokeActionDescriptor(controllerContext, actionDescriptor, asyncCallback, asyncState);

                    EndInvokeDelegate endDelegate = delegate(IAsyncResult asyncResult)
                    {
                        if (!asyncInvoker.EndInvokeActionDescriptor(asyncResult))
                        {
                            throw new HttpException(404, "Unknown action");
                        }
                    };
                    result = AsyncResultWrapper.Begin(callback, state, beginDelegate, endDelegate, _executeCoreTag);
                }
                else
                {
                    throw new InvalidOperationException("Only ApiActionInvoker is supported at this time.");
                    // TODO: consider translation.
                    //Action action = delegate
                    //{
                    //    if (!invoker.InvokeAction(this.ControllerContext, actionName))
                    //    {
                    //        this.HandleUnknownAction(actionName);
                    //    }
                    //};
                    //result = AsyncResultWrapper.BeginSynchronous(callback, state, action, _executeCoreTag);
                }
            }
            catch
            {
                this.PossiblySaveTempData();
                throw;
            }
            return result;
        }

        protected override void EndExecuteCore(IAsyncResult asyncResult)
        {
            try
            {
                AsyncResultWrapper.End(asyncResult, _executeCoreTag);
            }
            finally
            {
                this.PossiblySaveTempData();
            }
        }

        protected override void ExecuteCore()
        {
            //TODO: same logic as BeginExecuteCore (Async)
            base.ExecuteCore();
        }

        #region TempData Imp

        internal void PossiblyLoadTempData()
        {
            if (!base.ControllerContext.IsChildAction)
            {
                base.TempData.Load(base.ControllerContext, this.TempDataProvider);
            }
        }

        internal void PossiblySaveTempData()
        {
            if (!base.ControllerContext.IsChildAction)
            {
                base.TempData.Save(base.ControllerContext, this.TempDataProvider);
            }
        } 
        #endregion

        private static readonly object _executeCoreTag;

        static ApiController()
        {
            _executeCoreTag = new object();
        }
    }
}
