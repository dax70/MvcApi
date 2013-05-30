namespace MvcApi//System.Web.Mvc
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Diagnostics.CodeAnalysis;
    /* From MVC Codeplex source code */
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Instances of this type are meant to be singletons.")]
    internal abstract class ReaderWriterCache<TKey, TValue>
    {

        private readonly Dictionary<TKey, TValue> _cache;
        private readonly ReaderWriterLockSlim _rwLock;

        protected ReaderWriterCache()
            : this(null)
        {
        }

        protected ReaderWriterCache(IEqualityComparer<TKey> comparer)
        {
            this._rwLock = new ReaderWriterLockSlim();
            this._cache = new Dictionary<TKey, TValue>(comparer);
        }

        protected Dictionary<TKey, TValue> Cache
        {
            get { return _cache; }
        }

        protected TValue FetchOrCreateItem(TKey key, Func<TValue> creator)
        {
            // first, see if the item already exists in the cache
            _rwLock.EnterReadLock();
            try
            {
                TValue existingEntry;
                if (_cache.TryGetValue(key, out existingEntry))
                {
                    return existingEntry;
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            // insert the new item into the cache
            TValue newEntry = creator();
            _rwLock.EnterWriteLock();
            try
            {
                TValue existingEntry;
                if (_cache.TryGetValue(key, out existingEntry))
                {
                    // another thread already inserted an item, so use that one
                    return existingEntry;
                }

                _cache[key] = newEntry;
                return newEntry;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

    }
}
