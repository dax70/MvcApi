//----------------------------------------------------------------
// Originally from Mvc Rest Futures
//----------------------------------------------------------------

namespace MvcApi
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Web;

    /// <summary>
    /// Extension methods that facilitate support for content negotiation and HTTP method overload.
    /// </summary>
    public static class HttpRequestBaseExtensions
    {
        /// <summary>
        /// The "X-Http-Method-Override" string used to override the HTTP method on a POST request.
        /// </summary>
        public const string XHttpMethodOverride = "X-Http-Method-Override";

        /// <summary>
        /// Returns the format of a given request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The format of the request.</returns>
        /// <exception cref="HttpException">If the format is unrecognized or not supported.</exception>
        public static ContentType GetRequestFormat(this HttpRequestBase request)
        {
            return HttpHelper.GetRequestFormat(request);
        }

        /// <summary>
        /// Returns the preferred content type to use for the response, based on the request, according to the following
        /// rules:
        /// 1. If the query string contains a key called "format", its value is returned as the content type
        /// 2. Otherwise, if the request has an Accepts header, the list of content types in order of preference is returned
        /// 3. Otherwise, if the request has a content type, its value is returned
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The formats to use for rendering a response.</returns>
        public static List<ContentType> GetResponseFormats(this HttpRequestBase request)
        {
            return HttpHelper.GetResponseFormats(request);
        }

        internal static bool HasBody(this HttpRequestBase request)
        {
            return request.ContentLength > 0 || string.Compare("chunked", request.Headers["Transfer-Encoding"], StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Determines whether the specified HTTP request was sent by a Browser. A request is considered to be from the browser
        /// if it's a GET or POST and has a known User-Agent header (as determined by the request's BrowserCapabilities property),
        /// and does not have a non-HTML entity format (XML/JSON)
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>true if the specified HTTP request is a Browser request; otherwise, false.</returns>
        public static bool IsBrowserRequest(this HttpRequestBase request)
        {
            switch (request.HttpMethod)
            {
                case "GET":
                case "POST":
                    break;
                default:
                    return false;
            }
            HttpBrowserCapabilitiesBase browserCapabilities = request.Browser;
            if (browserCapabilities != null && !string.IsNullOrEmpty(request.Browser.Browser) && request.Browser.Browser != "Unknown")
            {
                return true;
            }
            return false;
        }
    }
}
