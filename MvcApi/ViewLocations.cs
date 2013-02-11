namespace MvcApi
{
    public class ViewLocations
    {
        private static ViewLocationCollection _instance = new ViewLocationCollection();

        public static ViewLocationCollection Locations { get { return _instance; } }
    }
}
