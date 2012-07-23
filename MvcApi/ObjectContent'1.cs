namespace MvcApi
{
    using System;
    using MvcApi.Formatting;

    public class ObjectContent<T> : ObjectContent
    {

        public ObjectContent(T value, MediaTypeFormatter formatter, string mediaType)
            : base(value, formatter, mediaType)
        {
        }

    }
}
