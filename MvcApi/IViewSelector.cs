using System;
using System.Web.Mvc;

namespace MvcApi
{
    public interface IViewSelector
    {
        ViewResult SelectView(ViewLocationContext context);
    }
}
