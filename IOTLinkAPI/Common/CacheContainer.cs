using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace IOTLinkAPI.Common
{
    public class CacheContainer<TKey, TValue>
    {
        private static readonly double DEFAULT_TIMER_INTERVAL = 5 * 60 * 1000;
        public class CacheItem
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }

            public DateTime LastAccessed { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private Dictionary<TKey, CacheItem> _cache = new Dictionary<TKey, CacheItem>();

        private TimeSpan CacheAbsoluteExpiration { get; set; } = TimeSpan.FromSeconds(DEFAULT_TIMER_INTERVAL);

        private TimeSpan CacheSlidingExpiration { get; set; }

        private Timer _cleanupTimer;

        private readonly object _cacheLock = new object();

        public CacheContainer()
        {
            _cleanupTimer = new Timer(DEFAULT_TIMER_INTERVAL);
            _cleanupTimer.Elapsed += OnCleanupEvent;
            _cleanupTimer.Start();
        }

        private void OnCleanupEvent(object sender, ElapsedEventArgs e)
        {
            lock (_cacheLock)
            {
                _cache = _cache.Where(kvp => !IsExpired(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public TValue GetItem(TKey key, Func<TValue> createItem = null)
        {
            if (key == null)
                return default;

            if (!_cache.ContainsKey(key) || IsExpired(_cache[key]))
            {
                if (createItem == null)
                    return default;

                UpdateItem(key, createItem());
            }

            var cacheItem = _cache[key];
            cacheItem.LastAccessed = DateTime.UtcNow;

            return _cache[key].Value;
        }

        public bool UpdateItem(TKey key, TValue value)
        {
            if (value == null)
                return false;

            lock (_cacheLock)
            {
                _cache[key] = new CacheItem
                {
                    Key = key,
                    Value = value,
                    LastUpdated = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow
                };
            }

            return true;
        }

        public bool RemoveKey(TKey key)
        {
            return key != null && (!_cache.ContainsKey(key) || _cache.Remove(key));
        }

        private bool IsExpired(CacheItem item)
        {
            if (item == null)
                return false;

            return VerifyExpiredDate(CacheAbsoluteExpiration, item.LastUpdated) || VerifyExpiredDate(CacheSlidingExpiration, item.LastAccessed);
        }

        private bool VerifyExpiredDate(TimeSpan expiration, DateTime dateTime)
        {
            if (expiration == null || expiration.TotalSeconds == 0)
                return false;

            return ((DateTime.UtcNow - dateTime).TotalSeconds >= expiration.TotalSeconds);
        }
    }
}
