namespace MvcApi.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using MvcApi.Http;

    /// <summary> Class that provides <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" />s for a request or response from a media range. </summary>
    public sealed class MediaRangeMapping : MediaTypeMapping
    {
        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.MediaRangeMapping" /> class. </summary>
        /// <param name="mediaRange">The <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" /> that provides a description of the media range.</param>
        /// <param name="mediaType">The <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" /> to return on a match.</param>
        public MediaRangeMapping(MediaTypeHeaderValue mediaRange, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(mediaRange);
        }

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.MediaRangeMapping" /> class. </summary>
        /// <param name="mediaRange">The description of the media range.</param>
        /// <param name="mediaType">The media type to return on a match.</param>
        public MediaRangeMapping(string mediaRange, string mediaType)
            : base(mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaRange))
            {
                throw new ArgumentNullException("mediaRange");
            }
            this.Initialize(new MediaTypeHeaderValue(mediaRange));
        }

        private void Initialize(MediaTypeHeaderValue mediaRange)
        {
            if (mediaRange == null)
            {
                throw new ArgumentNullException("mediaRange");
            }
            // TODO: Investigate if needed.
            //if (!mediaRange.IsMediaRange())
            //{
            //    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SRResources.InvalidMediaRange, new object[] { mediaRange.ToString() }));
            //}
            this.MediaRange = mediaRange;
        }

        protected sealed override double OnTryMatchMediaType(HttpRequestMessage request)
        {
            return 0.0;
        }

        protected sealed override double OnTryMatchMediaType(HttpResponseMessage response)
        {
            IEnumerable<MediaTypeWithQualityHeaderValue> accept = response.RequestMessage.AcceptHeaders;
            if (accept != null)
            {
                foreach (MediaTypeWithQualityHeaderValue value2 in accept)
                {
                    if ((value2 != null) && MediaTypeHeaderValueEqualityComparer.EqualityComparer.Equals(this.MediaRange, value2))
                    {
                        return (value2.Quality.HasValue ? value2.Quality.Value : 1.0);
                    }
                }
            }
            return 0.0;
        }

        /// <summary> Gets the <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" /> describing the known media range. </summary>
        public MediaTypeHeaderValue MediaRange { get; private set; }
    }
}

