using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class CacheService
{
    private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    private readonly int _maxCacheSize = 100;
    private readonly object _lockObject = new object();

    public bool TryGetCached(string key, out string result)
    {
        lock (_lockObject)
        {
            string hashedKey = GetHash(key);
            if (_cache.TryGetValue(hashedKey, out var cached))
            {
                if (DateTime.Now - cached.Timestamp < _cacheExpiry)
                {
                    result = cached.Value;
                    cached.HitCount++;
                    return true;
                }
                _cache.Remove(hashedKey);
            }
            result = null;
            return false;
        }
    }

    public void AddToCache(string key, string result)
    {
        lock (_lockObject)
        {
            string hashedKey = GetHash(key);
            
            // 如果缓存已满，移除最少使用的项
            if (_cache.Count >= _maxCacheSize)
            {
                RemoveLeastUsed();
            }

            _cache[hashedKey] = new CacheEntry
            {
                Value = result,
                Timestamp = DateTime.Now,
                HitCount = 0
            };
        }
    }

    public void ClearCache()
    {
        lock (_lockObject)
        {
            _cache.Clear();
        }
    }

    private void RemoveLeastUsed()
    {
        string keyToRemove = null;
        int minHitCount = int.MaxValue;
        DateTime oldestTime = DateTime.MaxValue;

        foreach (var kvp in _cache)
        {
            if (kvp.Value.HitCount < minHitCount || 
                (kvp.Value.HitCount == minHitCount && kvp.Value.Timestamp < oldestTime))
            {
                minHitCount = kvp.Value.HitCount;
                oldestTime = kvp.Value.Timestamp;
                keyToRemove = kvp.Key;
            }
        }

        if (keyToRemove != null)
        {
            _cache.Remove(keyToRemove);
        }
    }

    private string GetHash(string input)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    private class CacheEntry
    {
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
        public int HitCount { get; set; }
    }
}
