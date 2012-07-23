using System;

namespace MvcApi
{
    public static class GlobalConfiguration
    {

        private static Lazy<ApiConfiguration> _configuration = new Lazy<ApiConfiguration>(
        () =>
        {
            // TODO: could add more initialization.
            return new ApiConfiguration();
        });

        public static ApiConfiguration Configuration
        {
            get { return _configuration.Value; }
        }
    }
}
