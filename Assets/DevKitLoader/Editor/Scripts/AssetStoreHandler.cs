using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevKitLoader
{
    public class AssetStoreHandler : ISourceHandler
    {
        private readonly ToolEntry entry;

        public AssetStoreHandler(ToolEntry entry)
        {
            this.entry = entry;
        }

        public Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}