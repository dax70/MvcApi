namespace MvcApi.Query
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Web;
    using MvcApi.Properties;

    internal static class ODataQueryDeserializer
    {
        public static IQueryable<T> Deserialize<T>(Uri uri)
        {
            if (uri == null)
            {
                throw Error.ArgumentNull("uri");
            }
            return (IQueryable<T>)Deserialize(typeof(T), uri);
        }

        /// <summary>
        /// Deserializes the query operations in the specified Uri and applies them
        /// to the specified IQueryable.
        /// </summary>
        /// <param name="query">The root query to compose the deserialized query over.</param>
        /// <param name="uri">The request Uri containing the query operations.</param>
        /// <returns>The resulting IQueryable with the deserialized query composed over it.</returns>
        public static IQueryable Deserialize(IQueryable query, Uri uri)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            if (uri == null)
            {
                throw Error.ArgumentNull("uri");
            }

            StructuredQuery structuredQuery = GetStructuredQuery(uri);

            return Deserialize(query, structuredQuery.QueryParts);
        }

        public static IQueryable Deserialize(Type elementType, Uri uri)
        {
            if (elementType == null)
            {
                throw Error.ArgumentNull("elementType");
            }
            if (uri == null)
            {
                throw Error.ArgumentNull("uri");
            }
            StructuredQuery serviceQuery = GetStructuredQuery(uri);
            return Deserialize(Array.CreateInstance(elementType, 0).AsQueryable(), serviceQuery.QueryParts);
        }

        internal static IQueryable Deserialize(IQueryable query, IEnumerable<IStructuredQueryPart> queryParts)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            if (queryParts == null)
            {
                throw Error.ArgumentNull("queryParts");
            }

            foreach (IStructuredQueryPart part in queryParts)
            {
                //query = part.ApplyTo(query);
                query = ApplyTo(query, part);
            }

            return query;
        }

        internal static StructuredQuery GetStructuredQuery(Uri uri)
        {
            if (uri == null)
            {
                throw Error.ArgumentNull("uri");
            }

            NameValueCollection queryPartCollection = HttpUtility.ParseQueryString(uri.Query);

            return GetStructuredQuery(queryPartCollection);
        }

        internal static StructuredQuery GetStructuredQuery(NameValueCollection queryString)
        {
            if (queryString == null)
            {
                throw Error.ArgumentNull("queryString");
            }
            List<IStructuredQueryPart> structuredQueryParts = new List<IStructuredQueryPart>();
            foreach (string queryPart in queryString)
            {
                if (queryPart == null || !queryPart.StartsWith("$", StringComparison.Ordinal))
                {
                    // not a special query string
                    continue;
                }

                foreach (string value in queryString.GetValues(queryPart))
                {
                    string queryOperator = queryPart.Substring(1);
                    if (!IsSupportedQueryOperator(queryOperator))
                    {
                        // skip any operators we don't support
                        continue;
                    }

                    StructuredQueryPart structuredQueryPart = new StructuredQueryPart(queryOperator, value);
                    structuredQueryParts.Add(structuredQueryPart);
                }
            }

            // Query parts for OData need to be ordered $filter, $orderby, $skip, $top. For this
            // set of query operators, they are already in alphabetical order, so it suffices to
            // order by operator name. In the future if we support other operators, this may need
            // to be reexamined.
            structuredQueryParts = structuredQueryParts.OrderBy(p => p.QueryOperator).ToList();

            StructuredQuery structuredQuery = new StructuredQuery()
            {
                QueryParts = structuredQueryParts,
            };

            return structuredQuery;
        }

        internal static bool IsSupportedQueryOperator(string queryOperator)
        {
            return queryOperator == "filter" || queryOperator == "orderby" ||
                   queryOperator == "skip" || queryOperator == "top";
        }

        private static IQueryable ApplyTo(IQueryable query, IStructuredQueryPart queryPart)
        {
            if (!IsSupportedQueryOperator(queryPart.QueryOperator))
            {
                throw Error.Argument("queryOperator", SR.InvalidQueryOperator, queryPart.QueryOperator);
            }
            switch (queryPart.QueryOperator)
            {
                case "filter":
                    try
                    {
                        query = DynamicQueryable.Where(query, queryPart.QueryExpression, queryResolver: null);
                    }
                    catch (ParseException e)
                    {
                        throw new ParseException(
                            Error.Format(SRResources.ParseErrorInClause, "$filter", e.Message));
                    }
                    break;
                case "orderby":
                    try
                    {
                        query = DynamicQueryable.OrderBy(query, queryPart.QueryExpression, queryResolver: null);
                    }
                    catch (ParseException e)
                    {
                        throw new ParseException(
                            Error.Format(SRResources.ParseErrorInClause, "$orderby", e.Message));
                    }
                    break;
                case "skip":
                    try
                    {
                        int skipCount = Convert.ToInt32(queryPart.QueryExpression, System.Globalization.CultureInfo.InvariantCulture);
                        if (skipCount < 0)
                        {
                            throw new ParseException(
                                    Error.Format(SRResources.PositiveIntegerExpectedForODataQueryParameter, "$skip", queryPart.QueryExpression));
                        }

                        query = DynamicQueryable.Skip(query, skipCount);
                    }
                    catch (FormatException e)
                    {
                        throw new ParseException(
                            Error.Format(SRResources.ParseErrorInClause, "$skip", e.Message));
                    }
                    break;
                case "top":
                    try
                    {
                        int topCount = Convert.ToInt32(queryPart.QueryExpression, System.Globalization.CultureInfo.InvariantCulture);
                        if (topCount < 0)
                        {
                            throw new ParseException(
                                Error.Format(SRResources.PositiveIntegerExpectedForODataQueryParameter, "$top", queryPart.QueryExpression));
                        }

                        query = DynamicQueryable.Take(query, topCount);
                    }
                    catch (FormatException e)
                    {
                        throw new ParseException(
                            Error.Format(SRResources.ParseErrorInClause, "$top", e.Message));
                    }
                    break;
            }

            return query;
        }
    }
}

