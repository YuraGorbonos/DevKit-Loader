using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DevKitLoader
{
    public class GitLabReleaseHandler : ISourceHandler
    {
        [Serializable]
        private class GitLabRelease
        {
            public GitLabAssets assets;
        }

        [Serializable]
        private class GitLabAssets
        {
            public GitLabLink[] links;
            public GitLabSource[] sources;
        }

        [Serializable]
        private class GitLabLink
        {
            public string name;
            public string url;
            public long size;
        }

        [Serializable]
        private class GitLabSource
        {
            public string format;
            public string url;
        }

        private readonly ToolEntry _entry;

        public GitLabReleaseHandler(ToolEntry entry)
        {
            _entry = entry;
        }

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
                onProgress?.Invoke("Получение последнего релиза GitLab...", 0.1f);
                var releaseInfo = await GetLatestReleaseInfoAsync(_entry.Url, cancellationToken);

                if (string.IsNullOrEmpty(releaseInfo.downloadUrl))
                {
                    throw new Exception($"Не найден .unitypackage или .zip в последнем релизе: {_entry.Url}");
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
                request.SetRequestHeader("User-Agent", DevKitLoaderCommon.UserAgent);
                request.SetRequestHeader("Accept", "application/json");

                if (hasCached && !string.IsNullOrEmpty(cachedEntry.tag))
                {
                    string sanitized = DevKitLoaderCommon.SanitizeHeaderValue(cachedEntry.tag);

                    if (!string.IsNullOrEmpty(sanitized))
                    {
                        request.SetRequestHeader("If-None-Match", sanitized);
                    }
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

                throw new Exception($"GitLab API ошибка: {request.error} (код {request.responseCode})");
            }
        }

        private (string downloadUrl, long size) ParseReleaseJson(string json)
        {
            var release = JsonUtility.FromJson<GitLabRelease>(json);

            if (release?.assets?.links != null)
            {
                foreach (var link in release.assets.links)
                {
                    string lowerName = link.name?.ToLower() ?? "";

                    if (lowerName.EndsWith(".unitypackage") || lowerName.EndsWith(".zip"))
                    {
                        return (link.url, link.size);
                    }
                }
            }

            if (release?.assets?.sources != null)
            {
                foreach (var source in release.assets.sources)
                {
                    string lowerFormat = source.format?.ToLower() ?? "";

                    if (lowerFormat == "zip")
                    {
                        return (source.url, 0);
                    }
                }
            }

            throw new Exception("В релизе нет подходящих ассетов (.unitypackage или .zip)");
        }

        private async Task DownloadFileAsync(string url, string destPath, Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            await DevKitLoaderCommon.DownloadFileAsync(url, destPath, onProgress, cancellationToken);
        }

        private string ParseRepoToApiUrl(string repoUrl)
        {
            string apiUrl = repoUrl.TrimEnd('/');
            int start = apiUrl.IndexOf("gitlab.com/", StringComparison.OrdinalIgnoreCase);

            if (start == -1)
            {
                throw new Exception("URL не содержит gitlab.com");
            }

            string path = apiUrl.Substring(start + "gitlab.com/".Length);

            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 4);
            }

            // Encode path parts for project id: group%2Fproject
            string encodedPath = path.Replace("/", "%2F");
            return $"{DevKitLoaderCommon.GitLabApiBase}{encodedPath}/releases/latest";
        }

        // Санитайзеры вынесены в DevKitLoaderCommon
    }
}