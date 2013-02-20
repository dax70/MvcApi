using System;

namespace MvcApi.Views
{
    public class ViewLocationMatch
    {
        public ViewLocationMatch()
        {
        }

        public ViewLocation Location { get; set; }

        public int PointsOfMatch { get; set; }

        internal void Incrememt()
        {
            ++this.PointsOfMatch;
        }
    }
}
