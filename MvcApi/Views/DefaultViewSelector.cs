using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            var action = context.ActionDescriptor.ActionName;
            var controller = context.ActionDescriptor.ControllerDescriptor.ControllerName;

            var matches = ComputeViewMatches(context, this.Locations);

            ViewLocation location = SelectBestLocation(matches);

            return this.Locations.FirstOrDefault();
        }

        private Collection<ViewLocationMatch> ComputeViewMatches(ViewLocationContext context, IEnumerable<ViewLocation> locations)
        {
            var httpVerb = context.RequestContext.GetHttpMethod();
            var matches = new Collection<ViewLocationMatch>();

            foreach (var location in locations)
            {
                ViewLocationMatch match;

                if (location.Verbs.Any(verb => verb.Equals(httpVerb, StringComparison.CurrentCultureIgnoreCase)))
                {
                    if ((match = MatchTypeViewMapping(context, location)) != null)
                    {
                        matches.Add(match);
                    }
                    else
                    {

                    }
                }
            }
            return matches;
        }

        private ViewLocation SelectBestLocation(Collection<ViewLocationMatch> matches)
        {
            throw new NotImplementedException();
        }

        private ViewLocationMatch MatchTypeViewMapping(ViewLocationContext context, ViewLocation location)
        {
            int qualityValue = 0;

            //foreach (var matchValue in location.MatchValues)
            //{
            //    if(matchValue.Key.Equals(context.
            //}

            return null;
        }
    }
}
