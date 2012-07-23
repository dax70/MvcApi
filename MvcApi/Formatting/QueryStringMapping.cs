namespace MvcApi.Formatting
{
    using System;
    using System.Collections.Specialized;
    using System.Net.Http.Headers;
    using MvcApi.Http;

    /// <summary> Class that provides <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" />s from query strings. </summary>
    public sealed class QueryStringMapping : MediaTypeMapping
    {
        private static readonly Type queryStringMappingType = typeof(QueryStringMapping);

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.QueryStringMapping" /> class. </summary>
        /// <param name="queryStringParameterName">The name of the query string parameter to match, if present.</param>
        /// <param name="queryStringParameterValue">The value of the query string parameter specified by queryStringParameterName.</param>
        /// <param name="mediaType">The <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" /> to use if the query parameter specified by queryStringParameterName is present and assigned the value specified by queryStringParameterValue.</param>
        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(queryStringParameterName, queryStringParameterValue);
        }

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.QueryStringMapping" /> class. </summary>
        /// <param name="queryStringParameterName">The name of the query string parameter to match, if present.</param>
        /// <param name="queryStringParameterValue">The value of the query string parameter specified by queryStringParameterName.</param>
        /// <param name="mediaType">The media type to use if the query parameter specified by queryStringParameterName is present and assigned the value specified by queryStringParameterValue.</param>
        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, string mediaType)
            : base(mediaType)
        {
            this.Initialize(queryStringParameterName, queryStringParameterValue);
        }

        private bool DoesQueryStringMatch(NameValueCollection queryString)
        {
            if (queryString != null)
            {
                foreach (string str in queryString.AllKeys)
                {
                    if (string.Equals(str, this.QueryStringParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        string a = queryString[str];
                        if (string.Equals(a, this.QueryStringParameterValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void Initialize(string queryStringParameterName, string queryStringParameterValue)
        {
            if (string.IsNullOrWhiteSpace(queryStringParameterName))
            {
                throw new ArgumentNullException("queryStringParameterName");
            }
            if (string.IsNullOrWhiteSpace(queryStringParameterValue))
            {
                throw new ArgumentNullException("queryStringParameterValue");
            }
            this.QueryStringParameterName = queryStringParameterName.Trim();
            this.QueryStringParameterValue = queryStringParameterValue.Trim();
        }

        protected sealed override double OnTryMatchMediaType(HttpRequestMessage request)
        {
            NameValueCollection queryString = request.QueryString;
            if (!this.DoesQueryStringMatch(queryString))
            {
                return 0.0;
            }
            return 1.0;
        }

        protected sealed override double OnTryMatchMediaType(HttpResponseMessage response)
        {
            NameValueCollection queryString = response.RequestMessage.QueryString;
            if (!this.DoesQueryStringMatch(queryString))
            {
                return 0.0;
            }
            return 1.0;
        }

        /// <summary> Gets the query string parameter name. </summary>
        public string QueryStringParameterName { get; private set; }

        /// <summary> Gets the query string parameter value. </summary>
        public string QueryStringParameterValue { get; private set; }
    }
}

