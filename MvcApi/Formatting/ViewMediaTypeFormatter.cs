namespace MvcApi.Formatting
{
    using System;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Web.Mvc;
    using MvcApi.Http;

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
            //var view = new ViewResult { ViewName = formatterContext.ActionDescriptor.ActionName };
            var view = new SafeViewResult { ViewName = formatterContext.ActionDescriptor.ActionName };

            if (returnValue != null)
            {
                view.ViewData.Model = returnValue;
            }
            view.ExecuteResult(formatterContext);
        }
    }
}
