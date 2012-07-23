// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using MvcApi.Properties;

namespace MvcApi.Query
{
    /// <summary>
    /// Represents a single query operator to be applied to a query
    /// </summary>
    internal class StructuredQueryPart : IStructuredQueryPart
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public StructuredQueryPart()
        {
        }

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="queryOperator">The query operator</param>
        /// <param name="expression">The query expression</param>
        public StructuredQueryPart(string queryOperator, string expression)
        {
            if (queryOperator == null)
            {
                throw Error.ArgumentNull("queryOperator");
            }

            if (expression == null)
            {
                throw Error.ArgumentNull("expression");
            }

            QueryOperator = queryOperator;
            QueryExpression = expression;
        }

        public string QueryOperator { get; set; }

        public string QueryExpression { get; set; }

        public IQueryable ApplyTo(IQueryable query)
        {
            switch (QueryOperator)
            {
                case "filter":
                    try
                    {
                        query = DynamicQueryable.Where(query, QueryExpression, queryResolver: null);
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
                        query = DynamicQueryable.OrderBy(query, QueryExpression, queryResolver: null);
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
                        int skipCount = Convert.ToInt32(QueryExpression, System.Globalization.CultureInfo.InvariantCulture);
                        if (skipCount < 0)
                        {
                            throw new ParseException(
                                    Error.Format(SRResources.PositiveIntegerExpectedForODataQueryParameter, "$skip", QueryExpression));
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
                        int topCount = Convert.ToInt32(QueryExpression, System.Globalization.CultureInfo.InvariantCulture);
                        if (topCount < 0)
                        {
                            throw new ParseException(
                                Error.Format(SRResources.PositiveIntegerExpectedForODataQueryParameter, "$top", QueryExpression));
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

        /// <summary>
        /// Returns a string representation of this <see cref="StructuredQueryPart"/>
        /// </summary>
        /// <returns>The string representation of this <see cref="StructuredQueryPart"/></returns>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}={1}", QueryOperator, QueryExpression);
        }

    }
}
