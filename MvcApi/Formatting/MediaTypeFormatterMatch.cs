namespace MvcApi.Formatting
{
    using System;
    using System.Net.Http.Headers;
    using MvcApi.Http;

    public class MediaTypeFormatterMatch
    {
        public const double Match = 1.0;
        public const double NoMatch = 0.0;

        public MediaTypeFormatterMatch(MediaTypeHeaderValue mediaType)
            : this(mediaType, 1.0)
        {
        }

        public MediaTypeFormatterMatch(MediaTypeHeaderValue mediaType, double quality)
        {
            this.MediaType = (mediaType == null) ? null : (((ICloneable)mediaType).Clone() as MediaTypeHeaderValue);
            this.Quality = quality;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeFormatterMatch"/> class.
        /// </summary>
        /// <param name="formatter">The matching formatter.</param>
        /// <param name="mediaType">The media type. Can be <c>null</c> in which case the media type <c>application/octet-stream</c> is used.</param>
        /// <param name="quality">The quality of the match. Can be <c>null</c> in which case it is considered a full match with a value of 1.0</param>
        /// <param name="ranking">The kind of match.</param>
        public MediaTypeFormatterMatch(MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, double? quality, MediaTypeFormatterMatchRanking ranking)
        {
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            Formatter = formatter;
            MediaType = mediaType != null ? mediaType.Clone() : MediaTypeConstants.ApplicationOctetStreamMediaType;
            Quality = quality ?? FormattingUtilities.Match;
            Ranking = ranking;
        }

        /// <summary>
        /// Gets the media type formatter.
        /// </summary>
        public MediaTypeFormatter Formatter { get; private set; }

        /// <summary>
        /// Gets the matched media type.
        /// </summary>
        public MediaTypeHeaderValue MediaType { get; private set; }

        /// <summary>
        /// Gets the quality of the match
        /// </summary>
        public double Quality { get; private set; }

        /// <summary>
        /// Gets the kind of match that occurred.
        /// </summary>
        public MediaTypeFormatterMatchRanking Ranking { get; private set; }
    }
}
