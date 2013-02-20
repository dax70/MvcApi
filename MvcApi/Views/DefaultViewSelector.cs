using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Mvc;

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

        public ViewResult SelectView(ViewLocationContext context)
        {
            var action = context.ActionDescriptor.ActionName;
            var controller = context.ActionDescriptor.ControllerDescriptor.ControllerName;

            var matches = ComputeViewMatches(context, this.Locations);

            ViewLocation location = SelectBestLocation(matches);
            return new ViewResult { ViewName = location.ViewName };
        }

        private Collection<ViewLocationMatch> ComputeViewMatches(ViewLocationContext context, IEnumerable<ViewLocation> locations)
        {
            var matches = new Collection<ViewLocationMatch>();

            foreach (var location in locations)
            {
                if (!MatchsVerbMapping(context, location) && !MatchesActionName(context, location))
                {
                    continue;
                }

                var match = new ViewLocationMatch { Location = location };

                if (location.Type is IQueryable)
                {
                    match.Incrememt();
                }

                if(context.ReturnType.Equals(location.Type))
                {
                    match.Incrememt();
                }

                matches.Add(match);
            }
            return matches;
        }

        private ViewLocation SelectBestLocation(Collection<ViewLocationMatch> matches)
        {
            var match = matches.OrderByDescending(m => m.PointsOfMatch).FirstOrDefault();

            return match != null ? match.Location : null;
        }

        // Filter out if the verb doesnt match.
        private bool MatchsVerbMapping(ViewLocationContext context, ViewLocation location)
        {
            var httpVerb = context.RequestContext.GetHttpMethod();
            var verbs = location.Verbs;

            return verbs != null && !verbs.Any(verb => verb.Equals(httpVerb, StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesActionName(ViewLocationContext context, ViewLocation location)
        {
            return context.ActionDescriptor.ActionName.Equals(location.ActionName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
