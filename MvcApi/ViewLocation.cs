#region Using Directives
using System;
using System.Collections.Generic;
using System.Web.Routing; 
#endregion

namespace MvcApi
{
    public class ViewLocation
    {
        public ViewLocation()
        {
        }

        public string ActionName { get; set; }

        public bool IsCollection { get; set; }

        public IEnumerable<string> Verbs { get; set; }

        public RouteValueDictionary ActionParameters { get; set; }

        public Type Type { get; set; }

        public string ViewName { get; set; }
    }
}
