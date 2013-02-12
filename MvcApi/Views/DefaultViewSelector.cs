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

            var matches = new List<ViewLocationMatch>();

            foreach (var location in this.Locations)
            {
                ViewLocationMatch match;

                if (location.Verbs.Any(verb => verb.Equals(httpVerb, StringComparison.CurrentCultureIgnoreCase)))
                {
                    match = MatchViewMapping(context, location);
                }
            }

            return this.Locations.FirstOrDefault();
        }

        private ViewLocationMatch MatchViewMapping(ViewLocationContext locationContext, ViewLocation location)
        {
            return null;
        }
    }
}
