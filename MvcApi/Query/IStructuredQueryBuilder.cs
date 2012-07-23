namespace MvcApi.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A <see cref="IStructuredQueryBuilder"/> is used to build the query from (parsed) query parts.
    /// </summary>
    public interface IStructuredQueryBuilder
    {
        IQueryable ApplyQuery(IQueryable query, IEnumerable<IStructuredQueryPart> queryParts);
    }
}
