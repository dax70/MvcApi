namespace MvcApi.Formatting
{
    using System;
    using System.Net.Http.Headers;
    using MvcApi.Properties;
    using MvcApi.Http;

    public abstract class MediaTypeMapping
    {
        #region Constructors
        protected MediaTypeMapping(MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException("mediaType");
            }
            this.MediaType = mediaType;
        }

        protected MediaTypeMapping(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw new ArgumentNullException("mediaType");
            }
            this.MediaType = new MediaTypeHeaderValue(mediaType);
        } 
        #endregion

        public MediaTypeHeaderValue MediaType { get; private set; }

        public double TryMatchMediaType(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            return this.OnTryMatchMediaType(request);
        }
        
        public double TryMatchMediaType(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            if (response.RequestMessage == null)
            {
                throw new InvalidOperationException(String.Format(SRResources.ResponseMustReferenceRequest, httpResponseMessageType.Name, "response", httpRequestMessageType.Name, "RequestMessage"));
            }
            return this.OnTryMatchMediaType(response);
        }

        protected abstract double OnTryMatchMediaType(HttpResponseMessage response);

        protected abstract double OnTryMatchMediaType(HttpRequestMessage request);

        static MediaTypeMapping()
        {
            httpRequestMessageType = typeof(HttpRequestMessage);
            httpResponseMessageType = typeof(HttpResponseMessage);
        }

        private static readonly Type httpRequestMessageType;
        private static readonly Type httpResponseMessageType;
    }
}
