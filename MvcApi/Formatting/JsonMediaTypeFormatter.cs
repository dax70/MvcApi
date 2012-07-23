namespace MvcApi.Formatting
{
    #region Using Directives
    using System;
    using System.IO;
    using System.Net.Http.Headers;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using MvcApi.Http;
    #endregion

    public class JsonMediaTypeFormatter : StreamMediaTypeFormatter
    {
        private RequestHeaderMapping requestHeaderMapping;
        private IContractResolver _defaultContractResolver;
        private JsonSerializerSettings serializerSettings;

        public JsonMediaTypeFormatter()
        {
            this.ContentType = "application/json";

            // Set default supported media types
            foreach (MediaTypeHeaderValue value in supportedMediaTypes)
            {
                SupportedMediaTypes.Add(value);
            }

            // Initialize serializer
            _defaultContractResolver = new JsonContractResolver(this);
            serializerSettings = CreateDefaultSerializerSettings();

            // Set default supported character encodings
            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true));

            requestHeaderMapping = new XHRRequestHeaderMapping();
            MediaTypeMappings.Add(requestHeaderMapping);
        }

        public IContractResolver ContractResolver
        {
            get
            {
                if (_defaultContractResolver == null)
                {
                    _defaultContractResolver = new JsonContractResolver(this);
                }
                return _defaultContractResolver;
            }
            set
            {
                _defaultContractResolver = value;
                // Refresh settings;
                this.serializerSettings = this.CreateDefaultSerializerSettings();
            }
        }

        public bool Indent { get; set; }

        public bool UseDataContractSerializer { get; set; }

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

        public JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = this._defaultContractResolver,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                //TODO: consider these as options instead of hard coded here.
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public override void WriteToStream(Type type, object value, Stream stream, HttpRequestMessage requestMessage)
        {
            Encoding effectiveEncoding = this.SelectCharacterEncoding(requestMessage);

            if (TryGetDelegatingTypeForIQueryableGenericOrSame(ref type))
            {
                value = GetTypeRemappingConstructor(type).Invoke(new object[] { value });
            }
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(new StreamWriter(stream, effectiveEncoding)) { CloseOutput = false })
            {
                if (Indent)
                {
                    jsonTextWriter.Formatting = Formatting.Indented;
                }
                var serializer = JsonSerializer.Create(this.serializerSettings);
                serializer.Serialize(jsonTextWriter, value);
                jsonTextWriter.Flush();
            }
            // TODO: Consider enabling DataContractSerializer
            //if (!UseDataContractSerializer)
            //{
            //}
            //else
            //{
            //    DataContractJsonSerializer dataContractSerializer = this.GetDataContractSerializer(type);
            //    bool ownsStream = false;
            //    using (XmlWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, effectiveEncoding, ownsStream))
            //    {
            //        dataContractSerializer.WriteObject(writer, value);
            //    }
            //}
        }

        public void WriteTo(Type type, object value, TextWriter textWriter)
        {
            if (TryGetDelegatingTypeForIQueryableGenericOrSame(ref type))
            {
                value = MediaTypeFormatter.GetTypeRemappingConstructor(type).Invoke(new object[] { value });
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            var serializer = JsonSerializer.Create(jsonSerializerSettings);
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(textWriter))
            {
                serializer.Serialize(jsonTextWriter, value);
            }
        }

        private static readonly MediaTypeHeaderValue[] supportedMediaTypes = new MediaTypeHeaderValue[]
        {
            MediaTypeConstants.ApplicationJsonMediaType,
            MediaTypeConstants.TextJsonMediaType
        };

        public static MediaTypeHeaderValue DefaultMediaType
        {
            get { return MediaTypeConstants.ApplicationJsonMediaType; }
        }
    }
}
