using System.Collections.Generic;
using System.Linq;

namespace MvcApi.Query
{
    public class DefaultStructuredQueryBuilder : IStructuredQueryBuilder
    {
        public DefaultStructuredQueryBuilder()
        {
        }

        public virtual IQueryable ApplyQuery(IQueryable query, IEnumerable<IStructuredQueryPart> queryParts)
        {
            return ODataQueryDeserializer.Deserialize(query, queryParts);
        }
    }
}
