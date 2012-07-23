﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using MvcApi.Http;

namespace MvcApi.Formatting
{
    /// <summary>
    /// Class that selects a <see cref="MediaTypeFormatter"/> for an <see cref="HttpRequestMessage"/>
    /// or <see cref="HttpResponseMessage"/>.
    /// </summary>
    public class DefaultContentNegotiator : IContentNegotiator
    {
        public DefaultContentNegotiator()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultContentNegotiator"/> with
        /// the given setting for <paramref name="excludeMatchOnTypeOnly"/>.
        /// </summary>
        /// <param name="excludeMatchOnTypeOnly">
        /// If ExcludeMatchOnTypeOnly is true then we don't match on type only which means
        /// that we return null if we can't match on anything in the request. This is useful
        /// for generating 406 (Not Acceptable) status codes.
        /// </param>
        public DefaultContentNegotiator(bool excludeMatchOnTypeOnly)
        {
            ExcludeMatchOnTypeOnly = excludeMatchOnTypeOnly;
        }

        /// <summary>
        /// If ExcludeMatchOnTypeOnly is true then we don't match on type only which means
        /// that we return null if we can't match on anything in the request. This is useful
        /// for generating 406 (Not Acceptable) status codes.
        /// </summary>
        public bool ExcludeMatchOnTypeOnly { get; private set; }

        /// <summary>
        /// Performs content negotiating by selecting the most appropriate <see cref="MediaTypeFormatter"/> out of the passed in
        /// <paramref name="formatters"/> for the given <paramref name="request"/> that can serialize an object of the given
        /// <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to be serialized.</param>
        /// <param name="request">The request.</param>
        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
        /// <returns>The result of the negotiation containing the most appropriate <see cref="MediaTypeFormatter"/> instance,
        /// or <c>null</c> if there is no appropriate formatter.</returns>
        public virtual ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (formatters == null)
            {
                throw Error.ArgumentNull("formatters");
            }

            // If formatter list is empty then we won't find a match
            if (!formatters.Any())
            {
                return null;
            }

            // Go through each formatter to compute how well it matches.
            Collection<MediaTypeFormatterMatch> matches = ComputeFormatterMatches(type, request, formatters);

            // Select best formatter match among the matches
            MediaTypeFormatterMatch bestFormatterMatch = SelectResponseMediaTypeFormatter(matches);

            // We found a best formatter
            if (bestFormatterMatch != null)
            {
                // Find the best character encoding for the selected formatter
                //Encoding bestEncodingMatch = SelectResponseCharacterEncoding(request, bestFormatterMatch.Formatter);
                Encoding bestEncodingMatch = bestFormatterMatch.Formatter.SelectCharacterEncoding(request);
                if (bestEncodingMatch != null)
                {
                    bestFormatterMatch.MediaType.CharSet = bestEncodingMatch.WebName;
                }

                MediaTypeHeaderValue bestMediaType = bestFormatterMatch.MediaType;
                MediaTypeFormatter bestFormatter = bestFormatterMatch.Formatter.GetPerRequestFormatterInstance(type, request, bestMediaType);
                return new ContentNegotiationResult(bestFormatter, bestMediaType);
            }

            return null;
        }

        /// <summary>
        /// Determine how well each formatter matches by associating a <see cref="MediaTypeFormatterMatchRanking"/> value
        /// with the formatter. Then associate the quality of the match based on q-factors and other parameters. The result of this 
        /// method is a collection of the matches found categorized and assigned a quality value.
        /// </summary>
        /// <param name="type">The type to be serialized.</param>
        /// <param name="request">The request.</param>
        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
        /// <returns>A collection containing all the matches.</returns>
        protected virtual Collection<MediaTypeFormatterMatch> ComputeFormatterMatches(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (formatters == null)
            {
                throw Error.ArgumentNull("formatters");
            }

            IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues = null;

            // Go through each formatter to find how well it matches.
            Collection<MediaTypeFormatterMatch> matches = new Collection<MediaTypeFormatterMatch>();
            foreach (MediaTypeFormatter formatter in formatters)
            {
                MediaTypeFormatterMatch match = null;

                // Check first that formatter can write the actual type
                if (!formatter.CanWriteType(type))
                {
                    // Formatter can't even write the type so no match at all
                    continue;
                }

                // Match against media type mapping.
                if ((match = MatchMediaTypeMapping(request, formatter)) != null)
                {
                    matches.Add(match);
                    continue;
                }

                // Match against the accept header values.
                if (sortedAcceptValues == null)
                {
                    // Sort the Accept header values in descending order based on q-factor
                    sortedAcceptValues = SortMediaTypeWithQualityHeaderValuesByQFactor(request.AcceptHeaders);
                }
                if ((match = MatchAcceptHeader(sortedAcceptValues, formatter)) != null)
                {
                    matches.Add(match);
                    continue;
                }

                // Match against request's media type if any
                if ((match = MatchRequestMediaType(request, formatter)) != null)
                {
                    matches.Add(match);
                    continue;
                }

                // Match against the type of object we are writing out
                if ((match = MatchType(type, formatter)) != null)
                {
                    matches.Add(match);
                    continue;
                }
            }

            return matches;
        }

        /// <summary>
        /// Select the best match among the candidate matches found.
        /// </summary>
        /// <param name="matches">The collection of matches.</param>
        /// <returns>The <see cref="MediaTypeFormatterMatch"/> determined to be the best match.</returns>
        protected virtual MediaTypeFormatterMatch SelectResponseMediaTypeFormatter(ICollection<MediaTypeFormatterMatch> matches)
        {
            if (matches == null)
            {
                throw Error.ArgumentNull("matches");
            }

            MediaTypeFormatterMatch bestMatchOnType = null;
            MediaTypeFormatterMatch bestMatchOnAcceptHeaderLiteral = null;
            MediaTypeFormatterMatch bestMatchOnAcceptHeaderSubtypeMediaRange = null;
            MediaTypeFormatterMatch bestMatchOnAcceptHeaderAllMediaRange = null;
            MediaTypeFormatterMatch bestMatchOnMediaTypeMapping = null;
            MediaTypeFormatterMatch bestMatchOnRequestMediaType = null;

            // Go through each formatter to find the best match in each category.
            foreach (MediaTypeFormatterMatch match in matches)
            {
                switch (match.Ranking)
                {
                    case MediaTypeFormatterMatchRanking.MatchOnCanWriteType:
                        // First match by type trumps all other type matches
                        if (bestMatchOnType == null)
                        {
                            bestMatchOnType = match;
                        }
                        break;

                    case MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping:
                        // Matches on accept headers using mappings must choose the highest quality match
                        bestMatchOnMediaTypeMapping = UpdateBestMatch(bestMatchOnMediaTypeMapping, match);
                        break;

                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral:
                        // Matches on accept headers must choose the highest quality match.
                        // A match of 0.0 means we won't use it at all.
                        bestMatchOnAcceptHeaderLiteral = UpdateBestMatch(bestMatchOnAcceptHeaderLiteral, match);
                        break;

                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange:
                        // Matches on accept headers must choose the highest quality match.
                        // A match of 0.0 means we won't use it at all.
                        bestMatchOnAcceptHeaderSubtypeMediaRange = UpdateBestMatch(bestMatchOnAcceptHeaderSubtypeMediaRange, match);
                        break;

                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange:
                        // Matches on accept headers must choose the highest quality match.
                        // A match of 0.0 means we won't use it at all.
                        bestMatchOnAcceptHeaderAllMediaRange = UpdateBestMatch(bestMatchOnAcceptHeaderAllMediaRange, match);
                        break;

                    case MediaTypeFormatterMatchRanking.MatchOnRequestMediaType:
                        // First match on request content type trumps other request content matches
                        if (bestMatchOnRequestMediaType == null)
                        {
                            bestMatchOnRequestMediaType = match;
                        }
                        break;
                }
            }

            // If we received matches based on both supported media types and from media type mappings,
            // we want to give precedence to the media type mappings, but only if their quality is >= that of the supported media type.
            // We do this because media type mappings are the user's extensibility point and must take precedence over normal
            // supported media types in the case of a tie. The 99% case is where both have quality 1.0.
            if (bestMatchOnMediaTypeMapping != null)
            {
                MediaTypeFormatterMatch mappingOverride = bestMatchOnMediaTypeMapping;
                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderLiteral);
                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderSubtypeMediaRange);
                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderAllMediaRange);
                if (mappingOverride != bestMatchOnMediaTypeMapping)
                {
                    bestMatchOnMediaTypeMapping = null;
                }
            }

            // now select the formatter and media type
            // A MediaTypeMapping is highest precedence -- it is an extensibility point
            // allowing the user to override normal accept header matching
            MediaTypeFormatterMatch bestMatch = null;
            if (bestMatchOnMediaTypeMapping != null)
            {
                bestMatch = bestMatchOnMediaTypeMapping;
            }
            else if (bestMatchOnAcceptHeaderLiteral != null ||
                bestMatchOnAcceptHeaderSubtypeMediaRange != null ||
                bestMatchOnAcceptHeaderAllMediaRange != null)
            {
                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderLiteral);
                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderSubtypeMediaRange);
                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderAllMediaRange);
            }
            else if (bestMatchOnRequestMediaType != null)
            {
                bestMatch = bestMatchOnRequestMediaType;
            }
            else if (bestMatchOnType != null)
            {
                bestMatch = bestMatchOnType;
            }

            return bestMatch;
        }

        /// <summary>
        /// Determine the best character encoding for writing the response. First we look
        /// for accept-charset headers and if not found then we try to match
        /// any charset encoding in the request (in case of PUT, POST, etc.)
        /// If no encoding is found then we use the default for the formatter.
        /// </summary>
        /// <returns>The <see cref="Encoding"/> determined to be the best match.</returns>
        protected virtual Encoding SelectResponseCharacterEncoding(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            // If there are any SupportedEncodings then we pick an encoding
            if (formatter.SupportedEncodings.Count > 0)
            {
                // Sort Accept-Charset header values
                //IEnumerable<StringWithQualityHeaderValue> sortedAcceptCharsetValues = SortStringWithQualityHeaderValuesByQFactor(request.Headers.AcceptCharset);
                // TODO: Fix instead of using Formatter.SelectCharacterEncoding();
                IEnumerable<StringWithQualityHeaderValue> sortedAcceptCharsetValues = Enumerable.Empty<StringWithQualityHeaderValue>();

                // Check for match based on accept-charset headers
                foreach (StringWithQualityHeaderValue acceptCharset in sortedAcceptCharsetValues)
                {
                    foreach (Encoding encoding in formatter.SupportedEncodings)
                    {
                        if (encoding != null && acceptCharset.Quality != FormattingUtilities.NoMatch &&
                            (acceptCharset.Value.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase) ||
                            acceptCharset.Value.Equals("*", StringComparison.OrdinalIgnoreCase)))
                        {
                            return encoding;
                        }
                    }
                }

                // Check for match based on any request entity body
                return formatter.SelectCharacterEncoding(request);
            }

            return null;
        }

        /// <summary>
        /// Match a request against the <see cref="MediaTypeMapping"/>s registered with the formatter.
        /// </summary>
        /// <param name="request">The request to match.</param>
        /// <param name="formatter">The formatter to match against.</param>
        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
        protected virtual MediaTypeFormatterMatch MatchMediaTypeMapping(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            foreach (MediaTypeMapping mapping in formatter.MediaTypeMappings)
            {
                double quality;
                if (mapping != null && ((quality = mapping.TryMatchMediaType(request)) > FormattingUtilities.NoMatch))
                {
                    return new MediaTypeFormatterMatch(formatter, mapping.MediaType, quality, MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping);
                }
            }

            return null;
        }

        /// <summary>
        /// Match the request accept header field values against the formatter's registered supported media types.
        /// </summary>
        /// <param name="sortedAcceptValues">The sorted accept header values to match.</param>
        /// <param name="formatter">The formatter to match against.</param>
        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
        protected virtual MediaTypeFormatterMatch MatchAcceptHeader(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues, MediaTypeFormatter formatter)
        {
            if (sortedAcceptValues == null)
            {
                throw Error.ArgumentNull("sortedAcceptValues");
            }
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            foreach (MediaTypeWithQualityHeaderValue acceptMediaTypeValue in sortedAcceptValues)
            {
                foreach (MediaTypeHeaderValue supportedMediaType in formatter.SupportedMediaTypes)
                {
                    MediaTypeHeaderValueRange range;
                    if (supportedMediaType != null && acceptMediaTypeValue.Quality != FormattingUtilities.NoMatch &&
                        supportedMediaType.IsSubsetOf(acceptMediaTypeValue, out range))
                    {
                        MediaTypeFormatterMatchRanking ranking;
                        switch (range)
                        {
                            case MediaTypeHeaderValueRange.AllMediaRange:
                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange;
                                break;

                            case MediaTypeHeaderValueRange.SubtypeMediaRange:
                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange;
                                break;

                            default:
                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral;
                                break;
                        }

                        return new MediaTypeFormatterMatch(formatter, supportedMediaType, acceptMediaTypeValue.Quality, ranking);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Match any request media type (in case there is a request entity body) against the formatter's registered
        /// media types.
        /// </summary>
        /// <param name="request">The request to match.</param>
        /// <param name="formatter">The formatter to match against.</param>
        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
        protected virtual MediaTypeFormatterMatch MatchRequestMediaType(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            MediaTypeHeaderValue requestMediaType = request.ContentType;
            if (requestMediaType != null)
            {
                foreach (MediaTypeHeaderValue supportedMediaType in formatter.SupportedMediaTypes)
                {
                    if (supportedMediaType != null && supportedMediaType.IsSubsetOf(requestMediaType))
                    {
                        return new MediaTypeFormatterMatch(formatter, supportedMediaType, FormattingUtilities.Match, MediaTypeFormatterMatchRanking.MatchOnRequestMediaType);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Pick the first supported media type and indicate we've matched only on type
        /// If ExcludeMatchOnTypeOnly is true then we don't match on type only which means
        /// that we return null if we can't match on anything in the request. This is useful
        /// for generating 406 (Not Acceptable) status codes.
        /// </summary>
        /// <param name="type">The type to be serialized.</param>
        /// <param name="formatter">The formatter we are matching against.</param>
        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
        protected virtual MediaTypeFormatterMatch MatchType(Type type, MediaTypeFormatter formatter)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (formatter == null)
            {
                throw Error.ArgumentNull("formatter");
            }

            // We already know that we do match on type -- otherwise we wouldn't even be called --
            // so this is just a matter of determining how we match.
            if (!ExcludeMatchOnTypeOnly)
            {
                MediaTypeHeaderValue mediaType = formatter.SupportedMediaTypes.FirstOrDefault();
                return new MediaTypeFormatterMatch(formatter, mediaType, FormattingUtilities.Match, MediaTypeFormatterMatchRanking.MatchOnCanWriteType);
            }

            return null;
        }

        /// <summary>
        /// Sort Accept header values and related header field values with similar syntax rules 
        /// (if more than 1) in descending order based on q-factor.
        /// </summary>
        /// <param name="headerValues">The header values to sort.</param>
        /// <returns>The sorted header values.</returns>
        protected virtual IEnumerable<MediaTypeWithQualityHeaderValue> SortMediaTypeWithQualityHeaderValuesByQFactor(ICollection<MediaTypeWithQualityHeaderValue> headerValues)
        {
            if (headerValues == null)
            {
                throw Error.ArgumentNull("headerValues");
            }

            if (headerValues.Count > 1)
            {
                // Use OrderBy() instead of Array.Sort() as it performs fewer comparisons. In this case the comparisons
                // are quite expensive so OrderBy() performs better.
                return headerValues.OrderByDescending(m => m, MediaTypeWithQualityHeaderValueComparer.QualityComparer).ToArray();
            }
            else
            {
                return headerValues;
            }
        }

        /// <summary>
        /// Sort Accept-Charset, Accept-Encoding, Accept-Language and related header field values with similar syntax rules 
        /// (if more than 1) in descending order based on q-factor.
        /// </summary>
        /// <param name="headerValues">The header values to sort.</param>
        /// <returns>The sorted header values.</returns>
        protected virtual IEnumerable<StringWithQualityHeaderValue> SortStringWithQualityHeaderValuesByQFactor(ICollection<StringWithQualityHeaderValue> headerValues)
        {
            if (headerValues == null)
            {
                throw Error.ArgumentNull("headerValues");
            }

            if (headerValues.Count > 1)
            {
                // Use OrderBy() instead of Array.Sort() as it performs fewer comparisons. In this case the comparisons
                // are quite expensive so OrderBy() performs better.
                return headerValues.OrderByDescending(m => m, StringWithQualityHeaderValueComparer.QualityComparer).ToArray();
            }
            else
            {
                return headerValues;
            }
        }

        /// <summary>
        /// Evaluates whether a match is better than the current match and if so returns the replacement; otherwise returns the 
        /// current match.
        /// </summary>
        protected virtual MediaTypeFormatterMatch UpdateBestMatch(MediaTypeFormatterMatch current, MediaTypeFormatterMatch potentialReplacement)
        {
            if (potentialReplacement == null)
            {
                return current;
            }

            if (current != null)
            {
                return (potentialReplacement.Quality > current.Quality) ? potentialReplacement : current;
            }

            return potentialReplacement;
        }
    }
}
