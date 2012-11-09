namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using MvcApi.Formatting;
    using MvcApi.Services; 
    #endregion

    public class Configuration : IDisposable
    {
        private IDependencyResolver _dependencyResolver = Configuration.DefaultResolver();
        private readonly MediaTypeFormatterCollection _formatters = Configuration.DefaultFormatters();
        private List<IDisposable> _resourcesToDispose = new List<IDisposable>();
        private bool _disposed;

        public Configuration()
        {
            Services = new DefaultServices(this);
        }

        public IDependencyResolver DependencyResolver
        {
            get { return _dependencyResolver; }
            set
            {
                if (value == null)
                {
                    throw Error.PropertyNull();
                }
                _dependencyResolver = value;
            }
        }

        public MediaTypeFormatterCollection Formatters
        {
            get { return this._formatters; }
        }

        /// <summary>
        /// Gets the container of default services associated with this <see cref="HttpConfiguration"/>.
        /// Only supports the list of service types documented on <see cref="DefaultServices"/>. For general
        /// purpose types, please use <see cref="DependencyResolver"/>.
        /// </summary>
        public ServicesContainer Services { get; internal set; }

        /// <summary>
        /// Adds the given <paramref name="resource"/> to a list of resources that will be disposed once the configuration is disposed.
        /// </summary>
        /// <param name="resource">The resource to dispose. Can be <c>null</c>.</param>
        internal void RegisterForDispose(IDisposable resource)
        {
            if (resource == null)
            {
                return;
            }

            _resourcesToDispose.Add(resource);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    Services.Dispose();
                    //DependencyResolver.Dispose();

                    foreach (IDisposable resource in _resourcesToDispose)
                    {
                        resource.Dispose();
                    }
                }
            }
        }

        private static MediaTypeFormatterCollection DefaultFormatters()
        {
            return new MediaTypeFormatterCollection
            {
                //new JQueryMvcFormUrlEncodedFormatter()
            };
        }

        private static IDependencyResolver DefaultResolver()
        {
            return System.Web.Mvc.DependencyResolver.Current;
        }
    }
}
