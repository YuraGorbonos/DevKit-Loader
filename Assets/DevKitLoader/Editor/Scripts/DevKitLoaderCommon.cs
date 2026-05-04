using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
 
namespace DevKitLoader
{
    // Общий набор утилит и констант для DRY-модерации кода редакторской части
    public static class DevKitLoaderCommon
    {
        public const string UserAgent = "DevKitLoader";
        public const string GitHubApiBase = "https://api.github.com/repos/";
        public const string GitLabApiBase = "https://gitlab.com/api/v4/projects/";

        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "Unknown";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            foreach (char c in Path.GetInvalidPathChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }

        public static string SanitizeHeaderValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            // Разрешённые символы: буквы, цифры, пробелы, дефис, подчёркивание, двоеточие, точка, звёздочка
            return Regex.Replace(value, @"[^\w\-\s:\.\*]", "");
        }

        public static string GetTargetFolderForName(string name)
        {
            return $"Assets/DevKitInstalled/{SanitizeFolderName(name)}";
        }

        public static async Task DownloadFileAsync(string url, string destPath, Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(destPath);
                var asyncOp = request.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        throw new OperationCanceledException();
                    }

                    onProgress?.Invoke("Скачивание...", 0.4f + asyncOp.progress * 0.4f);
                    await Task.Delay(50, cancellationToken);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Ошибка загрузки: {request.error}");
                }
            }
        }
    }
}
