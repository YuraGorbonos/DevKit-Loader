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
                    // Проверяем размер файла кэша
                    FileInfo fileInfo = new FileInfo(_cachePath);

                    if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
                    {
                        Debug.LogWarning("[DevKitLoader] API cache file is too large, clearing it");
                        File.Delete(_cachePath);
                        return new Dictionary<string, ApiCacheEntry>();
                    }

                    string json = File.ReadAllText(_cachePath);
                    var data = JsonUtility.FromJson<ApiCacheData>(json);

                    if (data?.entries != null)
                    {
                        var dict = new Dictionary<string, ApiCacheEntry>();

                        foreach (var pair in data.entries)
                        {
                            // Проверяем, что запись не повреждена
                            if (pair.key != null && pair.value != null)
                            {
                                dict[pair.key] = pair.value;
                            }
                        }

                        return dict;
                    }

                    return new Dictionary<string, ApiCacheEntry>();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DevKitLoader] Failed to load API cache: {e.Message}");

                    // При ошибке очищаем кэш
                    try
                    {
                        File.Delete(_cachePath);
                    }
                    catch
                    {
                        // Игнорируем ошибки при удалении
                    }

                    return new Dictionary<string, ApiCacheEntry>();
                }
            }

            return new Dictionary<string, ApiCacheEntry>();
        }

        public static void Save(Dictionary<string, ApiCacheEntry> entries)
        {
            try
            {
                // Проверяем размер кэша перед сохранением
                if (entries != null && entries.Count > 1000) // Ограничиваем количество записей
                {
                    Debug.LogWarning("[DevKitLoader] API cache has too many entries, cleaning up");

                    // Удаляем старые записи (оставляем только последние 500)
                    var sortedEntries = new List<KeyValuePair<string, ApiCacheEntry>>(entries);

                    sortedEntries.Sort((x, y) =>
                    {
                        if (x.Value.expiry == null || y.Value.expiry == null)
                        {
                            return 0;
                        }

                        return DateTime.TryParse(x.Value.expiry, out var xTime) &&
                               DateTime.TryParse(y.Value.expiry, out var yTime)
                                   ? xTime.CompareTo(yTime)
                                   : 0;
                    });

                    // Оставляем только последние 500 записей
                    var cleanedEntries = new Dictionary<string, ApiCacheEntry>();
                    int count = 0;

                    foreach (var entry in sortedEntries)
                    {
                        if (count >= 500)
                        {
                            break;
                        }

                        cleanedEntries[entry.Key] = entry.Value;
                        count++;
                    }

                    entries = cleanedEntries;
                }

                var data = new ApiCacheData();

                if (entries != null)
                {
                    var list = new List<ApiCacheEntryPair>();

                    foreach (var kv in entries)
                    {
                        // Проверяем, что запись корректна
                        if (kv.Key != null && kv.Value != null)
                        {
                            list.Add(new ApiCacheEntryPair { key = kv.Key, value = kv.Value });
                        }
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