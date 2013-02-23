using System;
using System.Linq;

namespace MvcApi.Views
{
    public static class ViewConfigurationExtensions
    {
        public static void EnableDefaultViews(this Configuration configuration, ViewLocationCollection locations)
        {
            locations.MapLocation("get", "index", isCollection: true);
            locations.MapLocation("get", "details", new { id = "" }); // Rails: show
        }
    }
}
