using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcApi.Collections
{
    /// <summary>
    /// Useful when wrapping IEnumerables such as Arrays, List and collections is needed for serializers.
    /// Also, "items" prevents security attacks such as JSON hijacking. 
    /// </summary>
    /// <remarks>This class should only be applied is trying to serialize a root enumerable.</remarks>
    /// <typeparam name="T"></typeparam>
    public sealed class QueryableWrapper<T>
    {
        public QueryableWrapper(IEnumerable<T> items)
        {
            this.Items = items;
        }

        public IEnumerable<T> Items { get; set; }
    }
}
