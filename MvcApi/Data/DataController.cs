using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MvcApi.Data
{
    public class DataController : ApiController
    {
        public DataController()
        {
        }

        protected override IActionInvoker CreateActionInvoker()
        {
            return new DataControllerActionInvoker();
        }
    }
}
