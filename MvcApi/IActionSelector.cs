namespace MvcApi
{
    using System;
    using System.Linq;
    using System.Web.Mvc;

    public interface IActionSelector
    {
        ILookup<string, ApiActionDescriptor> GetActionMapping(ControllerDescriptor controllerDescriptor);

        ActionDescriptor SelectAction(ControllerContext controllerContext);
    }
}
