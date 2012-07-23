namespace MvcApi.Http
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Web;
    using MvcApi.Properties;
    #endregion

    public class HttpResponseMessage
    {
        private MediaTypeHeaderValue contentType;
        private string statusDescription;
        private HttpStatusCode statusCode;
        private Dictionary<string, IEnumerable<string>> contentHeaders;

        public HttpResponseMessage()
        {
        }

        internal HttpResponseMessage(HttpContextBase httpContext)
        {
            this.HttpContext = httpContext;
        }

        #region Properties

        public ObjectContent Content { get; set; }

        public MediaTypeHeaderValue ContentType
        {
            get
            {
                if (this.contentType == null)
                {
                    this.contentType = new MediaTypeHeaderValue(this.HttpContext.Response.ContentType);
                }
                return this.contentType;
            }
        }

        public HttpRequestMessage RequestMessage { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return this.statusCode; }
            set
            {
                if (value < (HttpStatusCode)0 || value > (HttpStatusCode)0x3e7)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.statusCode = value;
            }
        }

        public string StatusDescription
        {
            get
            {
                if (this.statusDescription != null)
                {
                    return this.statusDescription;
                }
                return HttpStatusDescription.Get(this.StatusCode);
            }
            set
            {
                if ((value != null) && this.ContainsNewLineCharacter(value))
                {
                    throw new FormatException(SRResources.HttpStatusDescriptionFormatError);
                }
                this.statusDescription = value;
            }
        }

        internal IDictionary<string, IEnumerable<string>> ContentHeaders
        {
            get
            {
                if (this.contentHeaders == null)
                {
                    this.contentHeaders = new Dictionary<string, IEnumerable<string>>();
                    var headers = this.HttpContext.Response.Headers;
                    foreach (var key in headers.AllKeys)
                    {
                        this.contentHeaders.Add(key, headers.GetValues(key));
                    }
                }
                return this.contentHeaders;
            }
        }

        internal HttpContextBase HttpContext { get; set; }

        #endregion

        internal void AppendContentHeader(string headerName, string headerValue)
        {
            this.HttpContext.Response.AppendHeader(headerName, headerValue);
        }

        private bool ContainsNewLineCharacter(string value)
        {
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        return true;
                }
            }
            return false;
        }
    }
}
