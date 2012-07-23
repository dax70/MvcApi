namespace MvcApi//System.Web.Mvc
{
    using System;
    using System.Web.Mvc;
    /* From MVC Codeplex source code */
    internal sealed class ControllerDescriptorCache : ReaderWriterCache<Type, ControllerDescriptor>
    {
        public ControllerDescriptorCache()
        {
        }

        public ControllerDescriptor GetDescriptor(Type controllerType, Func<ControllerDescriptor> creator)
        {
            return base.FetchOrCreateItem(controllerType, creator);
        }

    }
}