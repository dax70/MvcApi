namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using MvcApi.Http;
    using System.Xml.Serialization;
    using System.Net.Http.Headers;
    #endregion

    public class XmlMediaTypeFormatter : StreamMediaTypeFormatter
    {
        private ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();

        public XmlMediaTypeFormatter()
        {
            this.ContentType = "application/xml";

            // Set default supported media types
            foreach (MediaTypeHeaderValue value in supportedMediaTypes)
            {
                SupportedMediaTypes.Add(value);
            }

            // Set default supported character encodings
            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true));
        }

        public override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            //if (FormattingUtilities.IsJsonValueType(type))
            //{
            //    return false;
            //}
            //if (this.UseDataContractSerializer)
            //{
            //    MediaTypeFormatter.TryGetDelegatingTypeForIQueryableGenericOrSame(ref type);
            //}
            //else
            //{
            //    MediaTypeFormatter.TryGetDelegatingTypeForIEnumerableGenericOrSame(ref type);
            //}
            //return (this.serializerCache.GetOrAdd(type, t => this.CreateDefaultSerializer(t, false)) != null);
            return true;
        }

        public override void WriteToStream(Type type, object value, Stream stream, HttpRequestMessage requestMessage)
        {
            if (TryGetDelegatingTypeForIQueryableGenericOrSame(ref type) ||
                TryGetDelegatingTypeForIEnumerableGenericOrSame(ref type))
            {
                value = GetTypeRemappingConstructor(type).Invoke(new object[] { value });
            }

            new XmlSerializer(type).Serialize(stream, value);
            //new DataContractSerializer(type).WriteObject(stream, value);
        }

        private static readonly MediaTypeHeaderValue[] supportedMediaTypes;

        private ConcurrentDictionary<Type, object> serializerCache;

        static XmlMediaTypeFormatter()
        {
            supportedMediaTypes = new MediaTypeHeaderValue[] { MediaTypeConstants.ApplicationXmlMediaType, MediaTypeConstants.TextXmlMediaType };
        }

    }
}
