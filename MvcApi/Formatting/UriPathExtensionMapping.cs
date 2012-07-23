namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net.Http.Headers;
    using MvcApi.Http;
    using MvcApi.Properties; 
    #endregion

    /// <summary> Class that provides <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" />s from path extensions appearing in a <see cref="T:System.Uri" />. </summary>
    public sealed class UriPathExtensionMapping : MediaTypeMapping
    {
        private static readonly Type uriPathExtensionMappingType = typeof(UriPathExtensionMapping);

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.UriPathExtensionMapping" /> class. </summary>
        /// <param name="uriPathExtension">The extension corresponding to mediaType. This value should not include a dot or wildcards.</param>
        /// <param name="mediaType">The <see cref="T:System.Net.Http.Headers.MediaTypeHeaderValue" /> that will be returned if uriPathExtension is matched.</param>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "There is no meaningful System.Uri representation for a path suffix such as '.xml'")]
        public UriPathExtensionMapping(string uriPathExtension, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(uriPathExtension);
        }

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.UriPathExtensionMapping" /> class. </summary>
        /// <param name="uriPathExtension">The extension corresponding to mediaType. This value should not include a dot or wildcards.</param>
        /// <param name="mediaType">The media type that will be returned if uriPathExtension is matched.</param>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "There is no meaningful System.Uri representation for a path suffix such as '.xml'")]
        public UriPathExtensionMapping(string uriPathExtension, string mediaType)
            : base(mediaType)
        {
            this.Initialize(uriPathExtension);
        }

        private static string GetUriPathExtensionOrNull(Uri uri)
        {
            if (uri == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SRResources.NonNullUriRequiredForMediaTypeMapping, new object[] { uriPathExtensionMappingType.Name }));
            }
            string str = null;
            int length = uri.Segments.Length;
            if (length > 0)
            {
                string str2 = uri.Segments[length - 1];
                int startIndex = str2.IndexOf('.') + 1;
                if ((startIndex > 0) && (startIndex < str2.Length))
                {
                    str = str2.Substring(startIndex);
                }
            }
            return str;
        }

        private void Initialize(string uriPathExtension)
        {
            if (string.IsNullOrWhiteSpace(uriPathExtension))
            {
                throw new ArgumentNullException("uriPathExtension");
            }
            this.UriPathExtension = uriPathExtension.Trim().TrimStart(new char[] { '.' });
        }

        protected sealed override double OnTryMatchMediaType(HttpRequestMessage request)
        {
            if (!string.Equals(GetUriPathExtensionOrNull(request.RequestUri), this.UriPathExtension, StringComparison.Ordinal))
            {
                return 0.0;
            }
            return 1.0;
        }

        protected sealed override double OnTryMatchMediaType(HttpResponseMessage response)
        {
            if (!string.Equals(GetUriPathExtensionOrNull(response.RequestMessage.RequestUri), this.UriPathExtension, StringComparison.Ordinal))
            {
                return 0.0;
            }
            return 1.0;
        }

        /// <summary> Gets the <see cref="T:System.Uri" /> path extension. </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "There is no meaningful System.Uri representation for a path suffix such as '.xml'")]
        public string UriPathExtension { get; private set; }
    }
}

