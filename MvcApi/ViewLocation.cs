using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace MvcApi
{
    public class ViewLocation
    {
        public ViewLocation()
        {
        }

        public IEnumerable<string> Verbs { get; set; }

        public RouteValueDictionary MatchValues { get; set; }

        public string View { get; set; }
    }
}
