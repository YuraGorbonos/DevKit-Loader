using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace DevKitLoader
{
    public class UpmHandler : ISourceHandler
    {
        private readonly ToolEntry _entry;

        public UpmHandler(ToolEntry entry)
        {
            _entry = entry;
        }

        public async Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            onProgress?.Invoke($"Добавление UPM пакета: {_entry.Name}", 0.2f);
            var tcs = new TaskCompletionSource<bool>();
            AddRequest request = Client.Add(_entry.Url);
            EditorApplication.update += PollRequest;

            void PollRequest()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    EditorApplication.update -= PollRequest;
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }
                if (request.IsCompleted)
                {
                    EditorApplication.update -= PollRequest;
                    if (request.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(true);
                    }
                    else if (request.Status >= StatusCode.Failure)
                    {
                        tcs.TrySetException(new Exception($"UPM ошибка: {request.Error?.message ?? "Unknown"}"));
                    }
                }
            }

            try
            {
                await tcs.Task;
                onProgress?.Invoke("Пакет успешно добавлен", 1f);
            }
            catch (OperationCanceledException)
            {
                onProgress?.Invoke("Отменено", 1f);
                throw;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Ошибка: {ex.Message}", 1f);
                throw;
            }
        }
    }
}
