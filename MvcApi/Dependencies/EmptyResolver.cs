// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MvcApi.Dependencies
{
    internal class EmptyResolver : IDependencyResolver
    {
        private static readonly IDependencyResolver _instance = new EmptyResolver();

        private EmptyResolver()
        {
        }

        public static IDependencyResolver Instance
        {
            get { return _instance; }
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType)
        {
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }
    }
}
