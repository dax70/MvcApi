namespace MvcApi.Http
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Web;
    #endregion

    internal static class HttpExtensions
    {
        private static readonly HashSet<string> httpContentHeaders;

        static HttpExtensions()
        {
            httpContentHeaders = new HashSet<string> { "Allow", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified" };
        }

        public static IEnumerable<MediaTypeWithQualityHeaderValue> AcceptHeaders(this HttpRequestBase request)
        {
            foreach (var header in request.AcceptTypes)
            {
                yield return new MediaTypeWithQualityHeaderValue(header);
            }
        }

        public static MediaTypeHeaderValue ContentTypeMedia(this HttpRequestBase request)
        {
            return new MediaTypeHeaderValue(request.ContentType);
        }

        public static MediaTypeHeaderValue ContentTypeMedia(this HttpResponseBase response)
        {
            return new MediaTypeHeaderValue(response.ContentType);
        }

        internal static HttpResponseMessage ConvertResponse(HttpContextBase httpContextBase, HttpRequestMessage request)
        {
            return null;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller becomes owner")]
        internal static HttpRequestMessage ConvertRequest(HttpContextBase httpContextBase)
        {
            HttpRequestBase request = httpContextBase.Request;
            //HttpMethod httpMethod = HttpMethod.GetHttpMethod(request.HttpMethod);
            Uri url = request.Url;
            //HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, url);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(request.HttpMethod, url);
            foreach (string header in request.Headers)
            {
                string[] headers = request.Headers.GetValues(header);
                httpRequestMessage.Headers.Add(header, headers);
            }
            httpRequestMessage.HttpContext = httpContextBase;
            return httpRequestMessage;
        }
    }
}
