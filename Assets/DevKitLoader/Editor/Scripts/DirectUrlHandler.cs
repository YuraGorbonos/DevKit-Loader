using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace DevKitLoader
{
    public class DirectUrlHandler : ISourceHandler
    {
        private readonly ToolEntry _entry;

        public DirectUrlHandler(ToolEntry entry)
        {
            _entry = entry;
        }

        public async Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            string url = _entry.Url;
            string fileName = Path.GetFileName(url);

            if (string.IsNullOrEmpty(fileName) || (!fileName.EndsWith(".unitypackage") && !fileName.EndsWith(".zip")))
            {
                throw new Exception("URL должен указывать на файл .unitypackage или .zip");
            }

            string tempFile = DownloadManager.GetTempFilePath(Path.GetExtension(fileName));
            onProgress?.Invoke($"Скачивание {fileName}...", 0.3f);
            await DownloadFileAsync(url, tempFile, onProgress, cancellationToken);
            onProgress?.Invoke("Скачивание завершено", 0.8f);

            string targetFolder = DevKitLoaderCommon.GetTargetFolderForName(_entry.Name);

            if (fileName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                PackageImporter.ExtractUnityPackage(tempFile, targetFolder);
            }
            else
            {
                PackageImporter.ExtractZip(tempFile, targetFolder);
            }

            onProgress?.Invoke("Установка завершена", 1f);
        }

        private async Task DownloadFileAsync(string url, string destPath, Action<string, float> onProgress, CancellationToken cancellationToken)
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

                    onProgress?.Invoke("Скачивание...", 0.3f + asyncOp.progress * 0.5f);
                    await Task.Delay(50, cancellationToken);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Ошибка загрузки: {request.error}");
                }
            }
        }

        // Санитайзеры вынесены в DevKitLoaderCommon
    }
}