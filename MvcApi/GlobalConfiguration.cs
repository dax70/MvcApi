using System;
using System.Web.Mvc;
using MvcApi.Services;

namespace MvcApi
{
    public static class GlobalConfiguration
    {
        private static Lazy<Configuration> _configuration = new Lazy<Configuration>(
        () =>
        {
            return new Configuration();
        });

        public static Configuration Configuration
        {
            get { return _configuration.Value; }
        }
    }
}
