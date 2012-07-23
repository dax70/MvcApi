namespace MvcApi.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using MvcApi.Properties;

    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Exception used only internally.")]
    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Justification = "Exception used only internally.")]
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Exception used only internally.")]
    internal class ParseException : Exception
    {
        public ParseException(string message, int position) : base(string.Format(CultureInfo.InvariantCulture, string.Format(CultureInfo.CurrentCulture, SR.ParseExceptionFormat, new object[] { message, position }), new object[0]))
        {
        }

        public ParseException(string message)
            : base(message)
        {
        }
    }
}

