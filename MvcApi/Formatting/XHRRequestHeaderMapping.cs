namespace MvcApi.Formatting
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using MvcApi.Http;

    internal sealed class XHRRequestHeaderMapping : RequestHeaderMapping
    {
        public XHRRequestHeaderMapping()
            : base("x-requested-with", "xmlhttprequest", StringComparison.OrdinalIgnoreCase, true, MediaTypeConstants.ApplicationJsonMediaType)
        {
        }

        protected override double OnTryMatchMediaType(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            var acceptHeaders = request.AcceptHeaders;
            if (acceptHeaders != null && acceptHeaders.Count() != 1 || !acceptHeaders.First<MediaTypeWithQualityHeaderValue>().MediaType.Equals("*/*", StringComparison.Ordinal))
            {
                return 0.0;
            }
            return base.OnTryMatchMediaType(request);
        }

        protected override double OnTryMatchMediaType(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            if (response.RequestMessage != null)
            {
                return this.OnTryMatchMediaType(response.RequestMessage);
            }
            return 0.0;
        }
    }
}
