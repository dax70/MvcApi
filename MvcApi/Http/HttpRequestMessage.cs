namespace MvcApi.Http
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Net.Mime;
    using System.Collections.Specialized;
    using System.Net.Http.Headers;
    #endregion

    public class HttpRequestMessage
    {
        private MediaTypeHeaderValue contentType;
        private Dictionary<string, IEnumerable<string>> headers;
        private List<MediaTypeWithQualityHeaderValue> acceptHeaders;
        private List<MediaTypeWithQualityHeaderValue> acceptCharset;
        private IDictionary<string, object> properties;
        private bool contentTypeParsed = false;

        #region Constructors

        public HttpRequestMessage()
            : this("GET", (Uri)null)
        {
        }

        public HttpRequestMessage(string method, Uri requestUri)
        {
            this.InitializeValues(method, requestUri);
        }

        public HttpRequestMessage(string method, string requestUri)
        {
            Uri uri = string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute);
            this.InitializeValues(method, uri);
        }
        #endregion

        #region Properties

        public ICollection<MediaTypeWithQualityHeaderValue> AcceptCharset
        {
            get
            {
                if (this.acceptCharset == null)
                {
                    var headers = this.HttpContext.Request.Headers["Accept-Charset"];
                    //this.acceptCharset = .Select(header => new MediaTypeWithQualityHeaderValue(header)).ToList();
                }
                return this.acceptCharset;
            }
        }

        public ICollection<MediaTypeWithQualityHeaderValue> AcceptHeaders
        {
            get
            {
                if (this.acceptHeaders == null)
                {
                    this.acceptHeaders = new List<MediaTypeWithQualityHeaderValue>();
                    foreach (var header in this.HttpContext.Request.AcceptTypes)
                    {
                        MediaTypeWithQualityHeaderValue mediaTypeHeader;
                        if (MediaTypeWithQualityHeaderValue.TryParse(header, out mediaTypeHeader))
                        {
                            this.acceptHeaders.Add(mediaTypeHeader);
                        }
                    }
                }
                return this.acceptHeaders;
            }
        }

        public MediaTypeHeaderValue ContentType
        {
            get
            {
                if (!contentTypeParsed & this.contentType == null && !string.IsNullOrEmpty(this.HttpContext.Request.ContentType))
                {
                    this.contentType = new MediaTypeHeaderValue(this.HttpContext.Request.ContentType);
                    contentTypeParsed = true;
                }
                return this.contentType;
            }
        }

        public IDictionary<string, IEnumerable<string>> Headers
        {
            get
            {
                if (this.headers == null)
                {
                    this.headers = new Dictionary<string, IEnumerable<string>>();
                }
                return this.headers;
            }
        }

        public string Method { get; set; }

        public Uri RequestUri { get; set; }

        public IDictionary<string, object> Properties
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new Dictionary<string, object>();
                }
                return this.properties;
            }
        }

        public NameValueCollection QueryString
        {
            get { return this.HttpContext.Request.QueryString; }
        }

        #endregion

        internal HttpContextBase HttpContext { get; set; }

        private void InitializeValues(string method, Uri requestUri)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }
            this.Method = method;
            this.RequestUri = requestUri;
        }
    }
}
