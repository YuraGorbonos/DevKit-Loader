using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DevKitLoader
{
    /// <summary>
    /// Обработчик для получения информации о релизах с GitHub
    /// </summary>
    public class GitHubReleaseHandler : ISourceHandler
    {
        /// <summary>
        /// Вложенные классы для JSON
        /// </summary>
        [Serializable]
        private class GitHubRelease
        {
            public GitHubAsset[] assets;
        }

        /// <summary>
        /// Класс ассета GitHub
        /// </summary>
        [Serializable]
        private class GitHubAsset
        {
            public string name;
            public string browserDownloadUrl;
            public long size;
        }

        private readonly ToolEntry _entry;

        /// <summary>
        /// Конструктор обработчика
        /// </summary>
        /// <param name="entry">Запись инструмента</param>
        public GitHubReleaseHandler(ToolEntry entry)
        {
            _entry = entry;
        }

        /// <summary>
        /// Устанавливает инструмент
        /// </summary>
        /// <param name="onProgress">Callback для отслеживания прогресса</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Задача для ожидания завершения установки</returns>
        public async Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            string downloadUrl = null;
            string fileName = null;

            if (IsDirectFileUrl(_entry.Url))
            {
                downloadUrl = _entry.Url;
                fileName = Path.GetFileName(downloadUrl);
                onProgress?.Invoke("Прямая ссылка на файл", 0.2f);
            }
            else
            {
                onProgress?.Invoke("Получение данных последнего релиза...", 0.1f);
                var releaseInfo = await GetLatestReleaseInfoAsync(_entry.Url, cancellationToken);

                if (string.IsNullOrEmpty(releaseInfo.downloadUrl))
                {
                    throw new Exception($"Не найден .unitypackage или .zip в релизе: {_entry.Url}");
                }

                downloadUrl = releaseInfo.downloadUrl;
                fileName = Path.GetFileName(downloadUrl);
                onProgress?.Invoke($"Найден ассет: {fileName}", 0.3f);
            }

            string tempFile = DownloadManager.GetTempFilePath(Path.GetExtension(fileName));
            onProgress?.Invoke($"Скачивание {fileName}...", 0.4f);
            await DownloadFileAsync(downloadUrl, tempFile, onProgress, cancellationToken);
            onProgress?.Invoke("Скачивание завершено", 0.8f);

            string targetFolder = DevKitLoaderCommon.GetTargetFolderForName(_entry.Name);

            if (fileName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                PackageImporter.ExtractUnityPackage(tempFile, targetFolder);
            }
            else if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                PackageImporter.ExtractZip(tempFile, targetFolder);
            }
            else
            {
                throw new Exception($"Неподдерживаемый тип файла: {fileName}");
            }

            onProgress?.Invoke("Установка завершена", 1f);
        }

        private bool IsDirectFileUrl(string url)
        {
            string lower = url.ToLower();
            return lower.EndsWith(".unitypackage") || lower.EndsWith(".zip");
        }

        private async Task<(string downloadUrl, long size, string etag)> GetLatestReleaseInfoAsync(string repoUrl, CancellationToken cancellationToken)
        {
            string apiUrl = ParseRepoToApiUrl(repoUrl);
            string cacheKey = apiUrl;

            var cache = ApiCache.Load();
            bool hasCached = cache.TryGetValue(cacheKey, out var cachedEntry) && !cachedEntry.IsExpired();

            using (var request = UnityWebRequest.Get(apiUrl))
            {
                // Устанавливаем заголовки с защитой от недопустимых символов
                request.SetRequestHeader("User-Agent", DevKitLoaderCommon.UserAgent);
                request.SetRequestHeader("Accept", "application/vnd.github.v3+json");

                if (hasCached && !string.IsNullOrEmpty(cachedEntry.tag))
                {
                    SetIfNoneMatchHeader(request, cachedEntry.tag, cache, cacheKey);
                }

                var asyncOp = request.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        throw new OperationCanceledException();
                    }

                    await Task.Delay(50, cancellationToken);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string etag = request.GetResponseHeader("ETag")?.Trim('"');
                    etag = DevKitLoaderCommon.SanitizeHeaderValue(etag); // очищаем
                    var (downloadUrl, size) = ParseReleaseJson(request.downloadHandler.text);

                    var newEntry = new ApiCacheEntry
                                   {
                                       tag = etag,
                                       downloadUrl = downloadUrl,
                                       size = size
                                   };

                    newEntry.SetExpiry(TimeSpan.FromHours(24));
                    cache[cacheKey] = newEntry;
                    ApiCache.Save(cache);
                    return (downloadUrl, size, etag);
                }

                if (request.responseCode == 304 && hasCached)
                {
                    return (cachedEntry.downloadUrl, cachedEntry.size, cachedEntry.tag);
                }

                throw new Exception($"GitHub API ошибка: {request.error} (код {request.responseCode})");
            }
        }

        private void SetIfNoneMatchHeader(UnityWebRequest request, string etag, Dictionary<string, ApiCacheEntry> cache, string cacheKey)
        {
            string sanitizedEtag = DevKitLoaderCommon.SanitizeHeaderValue(etag);

            if (!string.IsNullOrEmpty(sanitizedEtag))
            {
                try
                {
                    request.SetRequestHeader("If-None-Match", sanitizedEtag);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DevKitLoader] Invalid ETag header, clearing cache: {ex.Message}");

                    // Удаляем проблемную запись из кэша
                    cache.Remove(cacheKey);
                    ApiCache.Save(cache);
                }
            }
        }

        // Санитайзеры вынесены в DevKitLoaderCommon

        private (string downloadUrl, long size) ParseReleaseJson(string json)
        {
            var release = JsonUtility.FromJson<GitHubRelease>(json);

            if (release?.assets == null || release.assets.Length == 0)
            {
                throw new Exception("Релиз не содержит ассетов");
            }

            foreach (var asset in release.assets)
            {
                string lowerName = asset.name.ToLower();

                if (lowerName.EndsWith(".unitypackage") || lowerName.EndsWith(".zip"))
                {
                    return (asset.browserDownloadUrl, asset.size);
                }
            }

            throw new Exception("В релизе нет файлов .unitypackage или .zip");
        }

        private async Task DownloadFileAsync(string url, string destPath, Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            await DevKitLoaderCommon.DownloadFileAsync(url, destPath, onProgress, cancellationToken);
        }

        /// <summary>
        /// Парсит URL репозитория и преобразует его в API URL
        /// </summary>
        /// <param name="repoUrl">URL репозитория</param>
        /// <returns>API URL для последнего релиза</returns>
        private string ParseRepoToApiUrl(string repoUrl)
        {
            repoUrl = repoUrl.TrimEnd('/');
            const string githubBase = "github.com/";
            int start = repoUrl.IndexOf(githubBase, StringComparison.OrdinalIgnoreCase);

            if (start == -1)
            {
                throw new Exception("URL не содержит github.com");
            }

            string path = repoUrl.Substring(start + githubBase.Length);

            // Обрезаем всё после "/releases", "/tree/", "/blob/", "/raw/"
            int[] indices =
            {
                path.IndexOf("/releases", StringComparison.OrdinalIgnoreCase),
                path.IndexOf("/tree/", StringComparison.OrdinalIgnoreCase),
                path.IndexOf("/blob/", StringComparison.OrdinalIgnoreCase),
                path.IndexOf("/raw/", StringComparison.OrdinalIgnoreCase)
            };

            int minIndex = -1;

            foreach (int index in indices)
            {
                if (index > 0 && (minIndex == -1 || index < minIndex))
                {
                    minIndex = index;
                }
            }

            if (minIndex > 0)
            {
                path = path.Substring(0, minIndex);
            }

            // Убираем .git в конце
            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 4);
            }

            return $"{DevKitLoaderCommon.GitHubApiBase}{path}/releases/latest";
        }

        // Санитайзеры вынесены в DevKitLoaderCommon
    }
}