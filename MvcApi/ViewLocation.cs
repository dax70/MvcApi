using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MvcApi
{
    public class ViewLocation
    {
        public ViewLocation()
        {
        }

        public string ActionName { get; set; }

        public IEnumerable<string> Verbs { get; set; }

        public Type Type { get; set; }

        public string ViewName { get; set; }
    }
}
