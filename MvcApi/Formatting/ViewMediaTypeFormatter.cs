namespace MvcApi.Formatting
{
    using System;
    using System.Text;

    public class ViewMediaTypeFormatter : MediaTypeFormatter
    {
        public ViewMediaTypeFormatter()
        {
            this.ContentType = MediaTypeConstants.TextHtmlMediaType.ToString();

            SupportedMediaTypes.Add(MediaTypeConstants.TextHtmlMediaType);
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
            return true;
        }

        public override void ExecuteFormat(Type type, object returnValue, FormatterContext formatterContext)
        {
            IViewSelector viewSelector = GlobalConfiguration.Configuration.Services.GetViewSelector();

            var view = viewSelector.SelectView(new ViewLocationContext(formatterContext) 
            { 
                ActionDescriptor = formatterContext.ActionDescriptor,
                ReturnType = type
            });

            if (returnValue != null)
            {
                view.ViewData.Model = returnValue;
            }

            view.ExecuteResult(formatterContext);
        }
    }
}
