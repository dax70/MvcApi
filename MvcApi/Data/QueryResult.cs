namespace MvcApi.Data
{
    using System;
    using System.Collections;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the results of a query request along with its total count if requested.
    /// </summary>
    [DataContract]
    public class QueryResult : IVirtualList
    {
        public QueryResult(IEnumerable results, int totalCount)
        {
            this.Results = results;
            this.TotalCount = totalCount;
        }

        /// <summary>
        /// The results of the query.
        /// </summary>
        [DataMember]
        public IEnumerable Results { get; set; }

        /// <summary>
        /// The total count of the query, without any paging options applied.
        /// A TotalCount equal to -1 indicates that the count is equal to the
        /// result count.
        /// </summary>
        [DataMember]
        public int TotalCount { get; set; }
    }
}
