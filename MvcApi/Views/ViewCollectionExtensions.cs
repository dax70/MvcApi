using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;

namespace MvcApi.Views
{
    public static class ViewCollectionExtensions
    {
        public static ViewLocation MapLocation(this ViewLocationCollection locations, 
                string actioName, 
                string view, 
                object actionParameters = null, 
                Type type = null, 
                bool isCollection = false, 
                params string[] verbs)
        {
            var location = new ViewLocation 
            { 
                ActionName = actioName, 
                ViewName = view, 
                ActionParameters = new RouteValueDictionary(actionParameters), 
                Type = type, 
                IsCollection = isCollection, 
                Verbs = verbs 
            };

            locations.Add(location);

            return location;
        }
    }
}
