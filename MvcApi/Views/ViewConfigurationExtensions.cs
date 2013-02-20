using System;
using System.Linq;

namespace MvcApi.Views
{
    public static class ViewConfigurationExtensions
    {
        public static void EnableDefaultViews(this Configuration configuration, ViewLocationCollection locations)
        {
            locations.MapLocation("get", "index", type: typeof(IQueryable));
            locations.MapLocation("get", "details"); // Rails: show
        }
    }
}
