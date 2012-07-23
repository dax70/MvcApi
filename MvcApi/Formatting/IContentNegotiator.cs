using System;
using System.Collections.Generic;
using MvcApi.Http;

namespace MvcApi.Formatting
{
    public interface IContentNegotiator
    {
        ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters);
    }
}
