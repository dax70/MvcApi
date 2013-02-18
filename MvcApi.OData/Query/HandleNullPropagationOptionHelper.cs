using System;
using System.Web.Http.OData.Query;

namespace MvcApi.OData.Query
{
    internal static class HandleNullPropagationOptionHelper
    {
        public static bool IsDefined(HandleNullPropagationOption value)
        {
            if ((value != HandleNullPropagationOption.Default) && (value != HandleNullPropagationOption.True))
            {
                return (value == HandleNullPropagationOption.False);
            }
            return true;
        }

        public static void Validate(HandleNullPropagationOption value, string parameterValue)
        {
            if (!IsDefined(value))
            {
                throw Error.InvalidEnumArgument(parameterValue, (int)value, typeof(HandleNullPropagationOption));
            }
        }

    }
}
