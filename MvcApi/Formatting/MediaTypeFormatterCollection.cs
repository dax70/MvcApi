namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using MvcApi.Properties;
    #endregion

    /// <summary> Collection class that contains <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter" /> instances. </summary>
    public class MediaTypeFormatterCollection : Collection<MediaTypeFormatter>
    {
        private static readonly Type mediaTypeFormatterType = typeof(MediaTypeFormatter);

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatterCollection" /> class. </summary>
        public MediaTypeFormatterCollection()
            : this(CreateDefaultFormatters())
        {
        }

        /// <summary> Initializes a new instance of the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatterCollection" /> class. </summary>
        /// <param name="formatters">A collection of <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter" /> instances to place in the collection.</param>
        public MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter> formatters)
        {
            this.VerifyAndSetFormatters(formatters);
        }

        private static IEnumerable<MediaTypeFormatter> CreateDefaultFormatters()
        {
            return new MediaTypeFormatter[] 
            { 
                new ViewMediaTypeFormatter(), 
                new JsonMediaTypeFormatter(), 
                new XmlMediaTypeFormatter() 
            };
        }

        private void VerifyAndSetFormatters(IEnumerable<MediaTypeFormatter> formatters)
        {
            if (formatters == null)
            {
                throw new ArgumentNullException("formatters");
            }
            foreach (MediaTypeFormatter formatter in formatters)
            {
                if (formatter == null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SRResources.CannotHaveNullInList, new object[] { mediaTypeFormatterType.Name }), "formatters");
                }
                base.Add(formatter);
            }
        }

        /// <summary> Gets the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter" /> to use for application/x-www-form-urlencoded data. </summary>
        public ViewMediaTypeFormatter ViewFormatter
        {
            get { return base.Items.OfType<ViewMediaTypeFormatter>().FirstOrDefault(); }
        }

        /// <summary> Gets the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter" /> to use for Json. </summary>
        public JsonMediaTypeFormatter JsonFormatter
        {
            get { return base.Items.OfType<JsonMediaTypeFormatter>().FirstOrDefault(); }
        }

        /// <summary> Gets the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter" /> to use for Xml. </summary>
        public XmlMediaTypeFormatter XmlFormatter
        {
            get { return base.Items.OfType<XmlMediaTypeFormatter>().FirstOrDefault(); }
        }
    }
}

