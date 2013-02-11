using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcApi
{
    public interface IViewSelector
    {
        ViewLocation SelectLocation(ViewLocationContext context);
    }
}
