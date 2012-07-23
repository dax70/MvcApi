namespace MvcApi.Formatting
{
    using System;
    using System.ComponentModel;

    internal static class StringComparisonHelper
    {
        private static readonly Type stringComparisonType;

        static StringComparisonHelper()
        {
            stringComparisonType = typeof(StringComparison);
        }

        public static bool IsDefined(StringComparison value)
        {
            if ((((value != StringComparison.CurrentCulture) && (value != StringComparison.CurrentCultureIgnoreCase)) && ((value != StringComparison.InvariantCulture) && (value != StringComparison.InvariantCultureIgnoreCase))) && (value != StringComparison.Ordinal))
            {
                return (value == StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        public static void Validate(StringComparison value, string parameterName)
        {
            if (!IsDefined(value))
            {
                throw new InvalidEnumArgumentException(parameterName, (int)value, stringComparisonType);
            }
        }
    }
}
