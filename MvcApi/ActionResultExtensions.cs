namespace MvcApi
{
    using System;
    using System.Web.Mvc;

    public static class ActionResultExtensions
    {
        public static bool TryGetObjectValue<T>(this ActionResult result, out T value) where T : class
        {
            ObjectContent content = result as ObjectContent;
            if (content != null)
            {
                value = content.Value as T;
                return (((T)value) != null);
            }
            value = default(T);
            return false;
        }

        public static bool TrySetObjectValue<T>(this ActionResult result, T value) where T : class
        {
            ObjectContent content = result as ObjectContent;
            if (content != null)
            {
                try
                {
                    content.Value = value;
                }
                catch (ArgumentException)
                {
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
