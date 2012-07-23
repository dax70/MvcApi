namespace MvcApi
{
    using System;
    using System.Collections;

    public static class DictionaryExtensions
    {
        public static bool TryGetValue<T>(this IDictionary collection, string key, out T value)
        {
            if (collection == null)
            {
                throw Error.ArgumentNull("collection");
            }
            if (collection.Contains(key) && (value = (T)collection[key]) is T)
            {
                return true;
            }
            value = default(T);
            return false;
        }

    }
}
