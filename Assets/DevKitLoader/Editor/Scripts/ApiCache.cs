using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevKitLoader
{
    [Serializable]
    public class ApiCacheEntry
    {
        public string tag;
        public string downloadUrl;
        public long size;
        public string expiry; // ISO 8601

        public bool IsExpired()
        {
            if (DateTime.TryParse(this.expiry, out var expiry))
            {
                return DateTime.UtcNow > expiry;
            }

            return true;
        }

        public void SetExpiry(TimeSpan lifetime)
        {
            expiry = DateTime.UtcNow.Add(lifetime).ToString("o");
        }
    }

    [Serializable]
    public class ApiCacheEntryPair
    {
        public string key;
        public ApiCacheEntry value;
    }

    [Serializable]
    public class ApiCacheData
    {
        public ApiCacheEntryPair[] entries;
    }

    public static class ApiCache
    {
        private static readonly string _cachePath = Path.Combine(Application.dataPath, "../ProjectSettings/DevKitLoaderCache.json");

        public static Dictionary<string, ApiCacheEntry> Load()
        {
            if (File.Exists(_cachePath))
            {
                try
                {
                    string json = File.ReadAllText(_cachePath);
                    var data = JsonUtility.FromJson<ApiCacheData>(json);

                    if (data?.entries != null)
                    {
                        var dict = new Dictionary<string, ApiCacheEntry>();

                        foreach (var pair in data.entries)
                        {
                            dict[pair.key] = pair.value;
                        }

                        return dict;
                    }

                    return new Dictionary<string, ApiCacheEntry>();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DevKitLoader] Failed to load API cache: {e.Message}");
                    return new Dictionary<string, ApiCacheEntry>();
                }
            }

            return new Dictionary<string, ApiCacheEntry>();
        }

        public static void Save(Dictionary<string, ApiCacheEntry> entries)
        {
            try
            {
                var data = new ApiCacheData();

                if (entries != null)
                {
                    var list = new List<ApiCacheEntryPair>();

                    foreach (var kv in entries)
                    {
                        list.Add(new ApiCacheEntryPair { key = kv.Key, value = kv.Value });
                    }

                    data.entries = list.ToArray();
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_cachePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DevKitLoader] Failed to save API cache: {e.Message}");
            }
        }
    }
}