namespace MvcApi.Formatting
{
	#region Using Directives
	using System;
	using System.Collections.Generic;
	using System.Collections.Concurrent;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Net.Mime;
	using System.Reflection;
	using System.Text;
	using System.Web;
	using MvcApi.Http;
	using MvcApi.Properties;
    using System.Net.Http.Headers;
    using System.Collections.Specialized;
    using System.Configuration;
	#endregion

	public abstract class MediaTypeFormatter
	{
        private const int DefaultMinHttpCollectionKeys = 1;
        private const int DefaultMaxHttpCollectionKeys = 1000; // same default as ASPNET
        private const string IWellKnownComparerTypeName = "System.IWellKnownStringEqualityComparer, mscorlib, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089";

        private static readonly ConcurrentDictionary<Type, Type> _delegatingEnumerableCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, ConstructorInfo> _delegatingEnumerableConstructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();
        private static Lazy<int> _defaultMaxHttpCollectionKeys = new Lazy<int>(InitializeDefaultCollectionKeySize, true); // Max number of keys is 1000
        private static int _maxHttpCollectionKeys = -1;

		protected MediaTypeFormatter()
		{
			this.SupportedMediaTypes = new MediaTypeHeaderValueCollection();
			this.SupportedEncodings = new Collection<Encoding>();
			this.MediaTypeMappings = new Collection<MediaTypeMapping>();
		}

		#region Properties

		// TODO: Consider obsoleting.
		public string ContentType { get; set; }

		public Encoding Encoding { get; set; }

		public Collection<MediaTypeMapping> MediaTypeMappings { get; private set; }

		public Collection<MediaTypeHeaderValue> SupportedMediaTypes { get; private set; }

		public Collection<Encoding> SupportedEncodings { get; private set; }

		public IRequiredMemberSelector RequiredMemberSelector { get; set; }

		#endregion

		public abstract void ExecuteFormat(Type type, object returnValue, FormatterContext formatterContext);

        public abstract bool CanWriteType(Type type);

        /// <summary>
        /// Returns a specialized instance of the <see cref="MediaTypeFormatter"/> that can handle formatting a response for the given
        /// parameters. This method is called by <see cref="DefaultContentNegotiator"/> after a formatter has been selected through content
        /// negotiation.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <c>this</c> instance. Derived classes can choose to return a new instance if
        /// they need to close over any of the parameters.
        /// </remarks>
        /// <param name="type">The type being serialized.</param>
        /// <param name="request">The request.</param>
        /// <param name="mediaType">The media type chosen for the serialization. Can be <c>null</c>.</param>
        /// <returns>An instance that can format a response to the given <paramref name="request"/>.</returns>
        public virtual MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            return this;
        }

		/// <summary>
		/// Determines the best <see cref="Encoding"/> amongst the supported encodings
		/// for reading or writing an HTTP entity body based on the provided <paramref name="contentHeaders"/>.
		/// </summary>
		/// <param name="contentHeaders">The content headers provided as part of the request or response.</param>
		/// <returns>The <see cref="Encoding"/> to use when reading the request or writing the response.</returns>
		public Encoding SelectCharacterEncoding(HttpRequestMessage contentHeaders)
		{
			Encoding encoding = null;
			if (contentHeaders != null && contentHeaders.ContentType != null)
			{
				// Find encoding based on content type charset parameter
				string charset = contentHeaders.ContentType.CharSet;
				if (!String.IsNullOrWhiteSpace(charset))
				{
					encoding =
						SupportedEncodings.FirstOrDefault(
							enc => charset.Equals(enc.WebName, StringComparison.OrdinalIgnoreCase));
				}
			}

			if (encoding == null)
			{
				// We didn't find a character encoding match based on the content headers.
				// Instead we try getting the default character encoding.
				encoding = SupportedEncodings.FirstOrDefault();
			}

			if (encoding == null)
			{
				// No supported encoding was found so there is no way for us to start reading or writing.
				throw new InvalidOperationException(string.Format(SRResources.MediaTypeFormatterNoEncoding, GetType().Name));
			}

			return encoding;
		}

        //internal bool CanWriteAs(Type type, FormatterContext formatterContext, out MediaTypeHeaderValue matchedMediaType)
        //{
        //    if (type == null)
        //    {
        //        throw new ArgumentNullException("type");
        //    }
        //    if (formatterContext == null)
        //    {
        //        throw new ArgumentNullException("formatterContext");
        //    }
        //    if (!this.CanWriteType(type))
        //    {
        //        matchedMediaType = null;
        //        return false;
        //    }

        //    MediaTypeFormatterMatch mediaTypeMatch = null;

        //    HttpRequestMessage request = formatterContext.Request;
        //    MediaTypeHeaderValue mediaType = request != null ? request.ContentType : formatterContext.ContentType;

        //    if ((mediaType != null) && this.TryMatchSupportedMediaType(mediaType, out mediaTypeMatch))
        //    {
        //        matchedMediaType = mediaTypeMatch.MediaType;
        //        return true;
        //    }
        //    if ((request != null) && this.TryMatchMediaTypeMapping(request, out mediaTypeMatch))
        //    {
        //        matchedMediaType = mediaTypeMatch.MediaType;
        //        return true;
        //    }
        //    matchedMediaType = null;
        //    return false;
        //}

		internal IEnumerable<KeyValuePair<string, string>> GetResponseHeaders(Type objectType, string mediaType, HttpResponseMessage responseMessage)
		{
			return this.OnGetResponseHeaders(objectType, mediaType, responseMessage);
		}

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		protected virtual IEnumerable<KeyValuePair<string, string>> OnGetResponseHeaders(Type objectType, string mediaType, HttpResponseMessage responseMessage)
		{
			return null;
		}

		internal bool TryMatchMediaTypeMapping(HttpRequestMessage request, out MediaTypeFormatterMatch mediaTypeMatch)
		{
			foreach (MediaTypeMapping mapping in this.MediaTypeMappings)
			{
				double num;
				if ((mapping != null) && ((num = mapping.TryMatchMediaType(request)) > 0.0))
				{
					mediaTypeMatch = new MediaTypeFormatterMatch(mapping.MediaType, num);
					return true;
				}
			}
			mediaTypeMatch = null;
			return false;
		}

		internal bool TryMatchMediaTypeMapping(HttpResponseMessage response, out MediaTypeFormatterMatch mediaTypeMatch)
		{
			foreach (MediaTypeMapping mapping in this.MediaTypeMappings)
			{
				double quality;
				if ((mapping != null) && ((quality = mapping.TryMatchMediaType(response)) > 0.0))
				{
					mediaTypeMatch = new MediaTypeFormatterMatch(mapping.MediaType, quality);
					return true;
				}
			}
			mediaTypeMatch = null;
			return false;
		}

        //internal bool TryMatchSupportedMediaType(MediaTypeHeaderValue mediaType, out MediaTypeFormatterMatch mediaTypeMatch)
        //{
        //    foreach (MediaTypeHeaderValue supportedType in this.SupportedMediaTypes)
        //    {
        //        if (MediaTypeHeaderValueEqualityComparer.EqualityComparer.Equals(supportedType, mediaType))
        //        {
        //            MediaTypeWithQualityHeaderValue mediaTypeWithQuality = mediaType as MediaTypeWithQualityHeaderValue;
        //            double quality = ((mediaTypeWithQuality != null) && mediaTypeWithQuality.Quality.HasValue) ? mediaTypeWithQuality.Quality.Value : 1.0;
        //            mediaTypeMatch = new MediaTypeFormatterMatch(supportedType, quality);
        //            if (this.Encoding != null)
        //            {
        //                mediaTypeMatch.MediaType.CharSet = this.Encoding.WebName;
        //            }
        //            return true;
        //        }
        //    }
        //    mediaTypeMatch = null;
        //    return false;
        //}

        //internal bool TryMatchSupportedMediaType(IEnumerable<MediaTypeHeaderValue> mediaTypes, out MediaTypeFormatterMatch mediaTypeMatch)
        //{
        //    foreach (MediaTypeHeaderValue headerValue in mediaTypes)
        //    {
        //        if (this.TryMatchSupportedMediaType(headerValue, out mediaTypeMatch))
        //        {
        //            return true;
        //        }
        //    }
        //    mediaTypeMatch = null;
        //    return false;
        //}

        //internal ResponseMediaTypeMatch SelectResponseMediaType(Type type, FormatterContext formatterContext)
        //{
        //    if (type == null)
        //    {
        //        throw new ArgumentNullException("type");
        //    }
        //    if (formatterContext == null)
        //    {
        //        throw new ArgumentNullException("formatterContext");
        //    }
        //    MediaTypeHeaderValue mediaType = null;
        //    MediaTypeFormatterMatch mediaTypeMatch = null;
        //    if (!this.CanWriteType(type))
        //    {
        //        return null;
        //    }

        //    HttpResponseMessage response = formatterContext.Response;
        //    // Why look at Response ContentType when Accept header should take priority.
        //    //mediaType = response != null ? response.ContentType : null;

        //    //if ((mediaType != null) && this.TryMatchSupportedMediaType(mediaType, out mediaTypeMatch))
        //    //{
        //    //    return new ResponseMediaTypeMatch(mediaTypeMatch, ResponseFormatterSelectionResult.MatchOnResponseContentType);
        //    //}
        //    HttpRequestMessage requestMessage = (response != null) ? response.RequestMessage : formatterContext.Request;
        //    var request = formatterContext.HttpContext.Request;
        //    if (request != null)
        //    {
        //        IEnumerable<MediaTypeWithQualityHeaderValue> mediaTypes = requestMessage.AcceptHeaders.OrderBy<MediaTypeWithQualityHeaderValue, MediaTypeHeaderValue>(m => m, MediaTypeHeaderValueComparer.Comparer);

        //        if (this.TryMatchSupportedMediaType(mediaTypes, out mediaTypeMatch))
        //        {
        //            return new ResponseMediaTypeMatch(mediaTypeMatch, ResponseFormatterSelectionResult.MatchOnRequestAcceptHeader);
        //        }
        //        if (this.TryMatchMediaTypeMapping(response, out mediaTypeMatch))
        //        {
        //            return new ResponseMediaTypeMatch(mediaTypeMatch, ResponseFormatterSelectionResult.MatchOnRequestAcceptHeaderWithMediaTypeMapping);
        //        }
        //        var content = request.ContentType;
        //        if (content != null)
        //        {
        //            MediaTypeHeaderValue contentType = request.ContentTypeMedia();
        //            if ((contentType != null) && this.TryMatchSupportedMediaType(contentType, out mediaTypeMatch))
        //            {
        //                return new ResponseMediaTypeMatch(mediaTypeMatch, ResponseFormatterSelectionResult.MatchOnRequestContentType);
        //            }
        //        }
        //    }
        //    mediaType = this.SupportedMediaTypes.FirstOrDefault<MediaTypeHeaderValue>();
        //    if ((mediaType != null) && (this.Encoding != null))
        //    {
        //        mediaType = (MediaTypeHeaderValue)((ICloneable)mediaType).Clone();
        //        mediaType.CharSet = this.Encoding.WebName;
        //    }
        //    return new ResponseMediaTypeMatch(new MediaTypeFormatterMatch(mediaType), ResponseFormatterSelectionResult.MatchOnCanWriteType);
        //}

		#region Static Members

		private static ConcurrentDictionary<Type, Type> delegatingEnumerableCache;

		private static ConcurrentDictionary<Type, ConstructorInfo> delegatingEnumerableConstructorCache;

		static MediaTypeFormatter()
		{
			delegatingEnumerableCache = new ConcurrentDictionary<Type, Type>();
			delegatingEnumerableConstructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();
		}

		private static Type GetOrAddDelegatingType(Type type)
		{
			return delegatingEnumerableCache.GetOrAdd(type, delegate(Type typeToRemap)
			{
				Type elementType;
				if (typeToRemap.GetGenericTypeDefinition().Equals(EnumerableTypes.EnumerableInterfaceGenericType))
				{
					elementType = typeToRemap.GetGenericArguments()[0];
				}
				else
				{
					elementType = typeToRemap.GetInterface(EnumerableTypes.EnumerableInterfaceGenericType.FullName).GetGenericArguments()[0];
				}
				Type typeKey = EnumerableTypes.QueryableWrapperGenericType.MakeGenericType(new Type[] { elementType });
				ConstructorInfo constructor = typeKey.GetConstructor(new Type[]
				{
					EnumerableTypes.EnumerableInterfaceGenericType.MakeGenericType(new Type[]	{ elementType	})
				});
				delegatingEnumerableConstructorCache.TryAdd(typeKey, constructor);
				return typeKey;
			});
		}

		internal static ConstructorInfo GetTypeRemappingConstructor(Type type)
		{
			ConstructorInfo info = null;
			delegatingEnumerableConstructorCache.TryGetValue(type, out info);
			return info;
		}

		internal static bool TryGetDelegatingTypeForIEnumerableGenericOrSame(ref Type type)
		{
			return TryGetDelegatingType(EnumerableTypes.EnumerableInterfaceGenericType, ref type);
		}

		internal static bool TryGetDelegatingTypeForIQueryableGenericOrSame(ref Type type)
		{
			return TryGetDelegatingType(EnumerableTypes.QueryableInterfaceGenericType, ref type);
		}

		private static bool TryGetDelegatingType(Type interfaceType, ref Type type)
		{
			if (type != null 
				&& type.IsGenericType
				&& (type.GetInterface(interfaceType.FullName) != null || type.GetGenericTypeDefinition().Equals(interfaceType)))
			{
				type = GetOrAddDelegatingType(type);
				return true;
			}
			return false;
		}

		private static bool TryGetDelegatingType1(Type interfaceType, ref Type type)
		{
			if (type != null
				&& type.IsInterface
				&& type.IsGenericType
				&& (type.GetInterface(interfaceType.FullName) != null || type.GetGenericTypeDefinition().Equals(interfaceType)))
			{
				type = GetOrAddDelegatingType(type);
				return true;
			}

			return false;
		}

        private static int InitializeDefaultCollectionKeySize()
        {
            // we first detect if we are running on 4.5, return Max value if we are.
            Type comparerType = Type.GetType(IWellKnownComparerTypeName, throwOnError: false);

            if (comparerType != null)
            {
                return Int32.MaxValue;
            }

            // we should try to read it from the AppSettings 
            // if we found the aspnet settings configured, we will use that. Otherwise, we used the default 
            NameValueCollection settings = ConfigurationManager.AppSettings;
            int result;

            if (settings == null || !Int32.TryParse(settings["aspnet:MaxHttpCollectionKeys"], out result) || result < 0)
            {
                result = DefaultMaxHttpCollectionKeys;
            }

            return result;
        }
		#endregion
	}
}
