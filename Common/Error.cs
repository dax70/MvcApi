namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using MvcApi.Properties;
    using System.ComponentModel;
    #endregion

    internal static class Error
    {
        public static ArgumentException Argument(string parameterName, string messageFormat, params object[] messageArgs)
        {
            return new ArgumentException(Format(messageFormat, messageArgs), parameterName);
        }

        public static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName, object actualValue, string messageFormat, params object[] messageArgs)
        {
            return new ArgumentOutOfRangeException(parameterName, actualValue, Format(messageFormat, messageArgs));
        }

        public static ArgumentOutOfRangeException ArgumentMustBeGreaterThanOrEqualTo(string parameterName, object actualValue, object minValue)
        {
            return new ArgumentOutOfRangeException(parameterName, actualValue, Format(SRResources.ArgumentMustBeGreaterThanOrEqualTo, new object[] { minValue }));
        }

        public static ArgumentNullException ArgumentNull(string parameterName)
        {
            return new ArgumentNullException(parameterName);
        }

        public static ArgumentException AsyncCommon_InvalidAsyncResult(string parameterName)
        {
            return new ArgumentException(SRResources.AsyncCommon_InvalidAsyncResult, parameterName);
        }

        public static InvalidOperationException AsyncCommon_AsyncResultAlreadyConsumed()
        {
            return new InvalidOperationException(SRResources.AsyncCommon_AsyncResultAlreadyConsumed);
        }

        public static InvalidOperationException InvalidOperation(string messageFormat, params object[] messageArgs)
        {
            return new InvalidOperationException(Format(messageFormat, messageArgs));
        }

        public static ArgumentException InvalidEnumArgument(string parameterName, int invalidValue, Type enumClass)
        {
            return new InvalidEnumArgumentException(parameterName, invalidValue, enumClass);
        }

        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#", Justification = "Standard String.Format pattern and names.")]
        public static string Format(string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static ArgumentException ParameterCannotBeNullOrEmpty(string parameterName)
        {
            return new ArgumentException(SRResources.Common_NullOrEmpty, parameterName);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentNullException"/> with the provided properties.
        /// </summary>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "The purpose of this API is to return an error for properties")]
        public static ArgumentNullException PropertyNull()
        {
            return new ArgumentNullException("value");
        }

    }
}
