using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DevKitLoader
{
    public class AssetStoreHandler : ISourceHandler
    {
        private readonly ToolEntry _entry;

        public AssetStoreHandler(ToolEntry entry)
        {
            _entry = entry;
        }

        public Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            onProgress?.Invoke("Открытие страницы Asset Store...", 0.5f);
            Application.OpenURL(_entry.Url);
            onProgress?.Invoke("Открыто в браузере", 1f);
            return Task.CompletedTask;
        }
    }
}