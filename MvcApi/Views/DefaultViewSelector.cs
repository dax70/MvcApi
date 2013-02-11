using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace MvcApi.Views
{
    public class DefaultViewSelector : IViewSelector
    {
        private ViewLocationCollection locationCollection;

        public DefaultViewSelector()
        {
        }

        public ViewLocationCollection Locations
        {
            get
            {
                if (this.locationCollection == null)
                {
                    this.locationCollection = ViewLocations.Locations;
                }
                return this.locationCollection;
            }
            set
            {
                this.locationCollection = value;
            }
        }

        public ViewLocation SelectLocation(ViewLocationContext context)
        {
            var httpVerb = context.RequestContext.GetHttpMethod();
            var action = context.ActionDescriptor.ActionName;
            var controller = context.ActionDescriptor.ControllerDescriptor.ControllerName;


            // first filter by the verb
            var locations = this.Locations
                                .Where(location => location.Verbs.Any(verb => verb.Equals(httpVerb, StringComparison.CurrentCultureIgnoreCase)))
                                .Where(GetValue("action", action))
                                .Where(GetValue("controller", controller));

            return locations.FirstOrDefault();
        }

        private static Func<ViewLocation, bool> GetValue(string key, string value)
        {
            return location =>
                    {
                        object obj;
                        if (location.ViewTokens.TryGetValue(key, out obj) && obj != null)
                        {
                            return value.Equals(obj.ToString(), StringComparison.CurrentCultureIgnoreCase);
                        }
                        return true;
                    };
        }
    }
}
