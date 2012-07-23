namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using MvcApi.Http; 
    #endregion

    public class RequestHeaderMapping : MediaTypeMapping
    {
        public RequestHeaderMapping(string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(headerName, headerValue, valueComparison, isValueSubstring);
        }

        public RequestHeaderMapping(string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, string mediaType)
            : base(mediaType)
        {
            this.Initialize(headerName, headerValue, valueComparison, isValueSubstring);
        }

        public string HeaderName { get; private set; }

        public string HeaderValue { get; private set; }

        public StringComparison HeaderValueComparison { get; private set; }

        public bool IsValueSubstring { get; private set; }

        protected override double OnTryMatchMediaType(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            return MatchHeaderValue(request, this.HeaderName, this.HeaderValue, this.HeaderValueComparison, this.IsValueSubstring);
        }

        protected override double OnTryMatchMediaType(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            if (response.RequestMessage != null)
            {
                return MatchHeaderValue(response.RequestMessage, this.HeaderName, this.HeaderValue, this.HeaderValueComparison, this.IsValueSubstring);
            }
            return 0.0;
        }

        private void Initialize(string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentNullException("headerName");
            }
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                throw new ArgumentNullException("headerValue");
            }
            StringComparisonHelper.Validate(valueComparison, "valueComparison");
            this.HeaderName = headerName;
            this.HeaderValue = headerValue;
            this.HeaderValueComparison = valueComparison;
            this.IsValueSubstring = isValueSubstring;
        }
       
        private static double MatchHeaderValue(HttpRequestMessage request, string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring)        
        {
            IEnumerable<string> headerValues = request.Headers[headerName];
            if (headerValues != null)
            {
                foreach (string header in headerValues)
                {
                    if (isValueSubstring)
                    {
                        if (header.IndexOf(headerValue, valueComparison) != -1)
                        {
                            return 1.0;
                        }
                    }
                    else if (header.Equals(headerValue, valueComparison))
                    {
                        return 1.0;
                    }
                }
            }
            return 0.0;
        }
    }
}
