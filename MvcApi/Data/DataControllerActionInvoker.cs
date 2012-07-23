using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MvcApi.Data
{
    public class DataControllerActionInvoker: ApiControllerActionInvoker
    {
        public DataControllerActionInvoker()
        {
        }

        protected override void InvokeActionResult(ControllerContext controllerContext, ActionResult actionResult)
        {
            int totalCount;
            if (controllerContext.HttpContext.Items.TryGetValue(QueryFilterAttribute.TotalCountKey, out totalCount))
            {
                ObjectContent objectContent = actionResult as ObjectContent;
                IEnumerable results;
                if (objectContent != null && (results = (objectContent.Value as IEnumerable)) != null)
                {
                    objectContent.Value = new QueryResult(results, totalCount);
                }
            }
            base.InvokeActionResult(controllerContext, actionResult);
        }
    }
}
