namespace MvcApi.Http
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http.Headers; 
    #endregion

    internal class MediaTypeHeaderValueEqualityComparer : IEqualityComparer<MediaTypeHeaderValue>
    {
        private static readonly MediaTypeHeaderValueEqualityComparer mediaTypeEqualityComparer = new MediaTypeHeaderValueEqualityComparer();

        private MediaTypeHeaderValueEqualityComparer()
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This is part of implementing IEqualityComparer.")]
        public bool Equals(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2)
        {
            if (!string.Equals(mediaType1.MediaType, mediaType2.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (NameValueHeaderValue parameter1 in mediaType1.Parameters)
            {
                if (mediaType2.Parameters.FirstOrDefault((NameValueHeaderValue parameter2) => string.Equals(parameter1.Name, parameter2.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parameter1.Value, parameter2.Value, StringComparison.OrdinalIgnoreCase)) == null)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(MediaTypeHeaderValue mediaType)
        {
            return mediaType.MediaType.ToUpperInvariant().GetHashCode();
        }

        public static MediaTypeHeaderValueEqualityComparer EqualityComparer
        {
            get { return mediaTypeEqualityComparer; }
        }
    }
}

