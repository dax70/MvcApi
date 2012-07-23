using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MvcApi.Http;

namespace MvcApi.Formatting
{
    public abstract class StreamMediaTypeFormatter : MediaTypeFormatter
    {
        protected StreamMediaTypeFormatter()
        {
        }

        public abstract void WriteToStream(Type type, object value, Stream stream, HttpRequestMessage requestMessage);

        public override void ExecuteFormat(Type type, object returnValue, FormatterContext formatterContext)
        {
            this.WriteToStream(type, returnValue, formatterContext.HttpContext.Response.OutputStream, formatterContext.Request);
        }
    }
}
