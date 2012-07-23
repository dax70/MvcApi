using System;
using System.Linq;

namespace MvcApi.Query
{
    public interface IStructureQueryTransform
    {
        IQueryable ApplyTo(IQueryable query, IStructuredQueryPart queryPart);
    }
}
