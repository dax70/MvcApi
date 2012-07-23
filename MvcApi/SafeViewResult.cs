namespace MvcApi
{
    using System.Web.Mvc;

    internal class SafeViewResult : ViewResultBase
    {
        private string masterName;

        public SafeViewResult()
        {
        }

        public string MasterName
        {
            get { return (this.masterName ?? string.Empty); }
            set { this.masterName = value; }
        }

        protected override ViewEngineResult FindView(ControllerContext context)
        {
            ViewEngineResult result = base.ViewEngineCollection.FindView(context, base.ViewName, this.MasterName);
            if (result.View != null)
            {
                return result;
            }
            // TODO: Some default object view.
            result = base.ViewEngineCollection.FindView(context, "index", this.MasterName);
            return result;
        }
    }
}
