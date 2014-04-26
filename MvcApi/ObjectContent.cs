namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Web.Mvc;
    using MvcApi.Formatting;
    using MvcApi.Http;
    using MvcApi.Properties;
    #endregion

    public class ObjectContent : ActionResult
    {
        #region Fields
        private MediaTypeFormatterCollection formatters;
        private static readonly Type ObjectContentType;
        private static readonly Type MediaTypeFormatterType;
        #endregion

        #region Constructors

        public ObjectContent(object value, MediaTypeFormatter formatter, string mediaType)
            : this(value, formatter, BuildHeaderValue(mediaType))
        {
        }

        public ObjectContent(object value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value must not be null.");
            }
            this.Value = value;
            this.Formatter = formatter;
            this.MediaType = mediaType;
        }

        static ObjectContent()
        {
            ObjectContentType = typeof(ObjectContent);
            MediaTypeFormatterType = typeof(MediaTypeFormatter);
        }

        #endregion

        #region Properties

        private MediaTypeHeaderValue MediaType { get; set; }

        public Type ObjectType { get { return this.Value.GetType(); } }

        public object Value { get; set; }

        public MediaTypeFormatter Formatter { get; set; }

        protected internal FormatterContext FormatterContext { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We deliberately allow the entire collection to be set.")]
        protected internal MediaTypeFormatterCollection Formatters
        {
            get
            {
                if (this.formatters == null)
                {
                    this.formatters = new MediaTypeFormatterCollection();
                }
                return this.formatters;
            }
            set
            {
                this.formatters = value;
                this.WasFormatterCollectionSetExplicitly = true;
                this.ResetContentNegotiationResults();
            }
        }

        protected internal bool WasFormatterCollectionSetExplicitly { get; private set; }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TypeSet", Justification = "MediaTypeSet is not related to TypeSet")]
        protected internal bool WasMediaTypeSetExplicitly { get; set; }

        #endregion

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;
            var media = this.MediaType;
            // Set media type on the response object, useful if client specified several formats.
            response.ContentType = media != null && !string.IsNullOrEmpty(media.ToString()) ? media.ToString() : this.Formatter.ContentType;
            this.Formatter.ExecuteFormat(this.ObjectType, this.Value, this.FormatterContext);
        }

        private void ResetContentNegotiationResults()
        {
            if (!this.WasMediaTypeSetExplicitly)
            {
                this.MediaType = null;
            }
            this.Formatter = null;
        }

        internal static MediaTypeHeaderValue BuildHeaderValue(string mediaType)
        {
            return ((mediaType != null) ? new MediaTypeHeaderValue(mediaType) : null);
        }
    }
}
