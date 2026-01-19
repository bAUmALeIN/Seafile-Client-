using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WinFormsApp3.Data
{
    public class CacheManager
    {
        // Thread-Safe Dictionary
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly TimeSpan _validityPeriod = TimeSpan.FromMinutes(5);

        private class CacheEntry
        {
            public object Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public void Set(string key, object data)
        {
            var entry = new CacheEntry { Data = data, Timestamp = DateTime.Now };
            _cache.AddOrUpdate(key, entry, (k, old) => entry);
        }

        public T Get<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.Now - entry.Timestamp < _validityPeriod)
                {
                    return entry.Data as T;
                }
                else
                {
                    // Abgelaufen
                    _cache.TryRemove(key, out _);
                }
            }
            return null;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Invalidate(string key)
        {
            _cache.TryRemove(key, out _);
        }
    }
}