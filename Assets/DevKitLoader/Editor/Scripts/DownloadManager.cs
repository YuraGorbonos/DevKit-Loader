using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DevKitLoader
{
    public static class DownloadManager
    {
        private static readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "DevKitLoader");

        public static async Task InstallAssetsAsync(
            List<ToolEntry> entries,
            Action<string, float> onProgress,
            Action<string> onError,
            CancellationToken cancellationToken)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }

            int total = entries.Count;
            int current = 0;

            foreach (var entry in entries)
            {
                current++;

                if (cancellationToken.IsCancellationRequested)
                {
                    onProgress?.Invoke("Операция отменена пользователем", 1f);
                    break;
                }

                string toolName = string.IsNullOrEmpty(entry.Name) ? entry.Url : entry.Name;

                try
                {
                    onProgress?.Invoke($"Начинаем установку: {toolName} ({current}/{total})", 0f);

                    var handler = SourceHandlerFactory.CreateHandler(entry);

                    await handler.InstallAsync(
                        (msg, prog) => onProgress?.Invoke($"{toolName}: {msg}", (current - 1 + prog) / total),
                        cancellationToken);

                    onProgress?.Invoke($"Успешно: {toolName} ({current}/{total})", (float)current / total);
                    Debug.Log($"[DevKitLoader] Установлен: {toolName}");
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Ошибка при установке {toolName}: {ex.Message}";
                    onError?.Invoke(errorMsg);
                    Debug.LogError($"[DevKitLoader] {errorMsg}\n{ex.StackTrace}");
                }
            }

            try
            {
                if (Directory.Exists(_tempFolder))
                {
                    Directory.Delete(_tempFolder, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DevKitLoader] Не удалось очистить временную папку: {ex.Message}");
            }
        }

        internal static string GetTempFilePath(string extension = "")
        {
            string fileName = Guid.NewGuid().ToString("N") + extension;
            return Path.Combine(_tempFolder, fileName);
        }
    }
}