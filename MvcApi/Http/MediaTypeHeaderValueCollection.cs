namespace MvcApi.Http
{
    #region Using Directives
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Net.Http.Headers;
    using MvcApi.Properties; 
    #endregion

    internal class MediaTypeHeaderValueCollection : Collection<MediaTypeHeaderValue>
    {
        public MediaTypeHeaderValueCollection()
        {
        }

        protected override void InsertItem(int index, MediaTypeHeaderValue item)
        {
            ValidateMediaType(item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, MediaTypeHeaderValue item)
        {
            ValidateMediaType(item);
            base.SetItem(index, item);
        }

        private static readonly Type mediaTypeHeaderValueType = typeof(MediaTypeHeaderValue);

        private static void ValidateMediaType(MediaTypeHeaderValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            ParsedMediaTypeHeaderValue parsedValue = new ParsedMediaTypeHeaderValue(item);
            if (parsedValue.IsAllMediaRange || parsedValue.IsSubtypeMediaRange)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SRResources.CannotUseMediaRangeForSupportedMediaType, mediaTypeHeaderValueType.Name, item.MediaType), "item");
            }
        }
    }
}
