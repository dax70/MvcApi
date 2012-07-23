using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using MvcApi.Http;

namespace MvcApi.Formatting
{
    public class ContentNegotiationResult
    {
        private MediaTypeFormatter _formatter;

        public ContentNegotiationResult(MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }
            this._formatter = formatter;
            this.MediaType = mediaType;
        }

        public MediaTypeFormatter Formatter
        {
            get
            {
                return this._formatter;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this._formatter = value;
            }
        }

        public MediaTypeHeaderValue MediaType { get; set; }
    }
}
