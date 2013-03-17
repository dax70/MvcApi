using System.Web;
using System.Web.Mvc;
using MvcApi.Filters;

namespace MvcApi.Movies
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            filters.Add(new QueryableFilterProvider());
        }
    }
}