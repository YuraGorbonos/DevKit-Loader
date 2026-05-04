using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevKitLoader
{
    public class DirectUrlHandler : ISourceHandler
    {
        private readonly ToolEntry entry;

        public DirectUrlHandler(ToolEntry entry)
        {
            this.entry = entry;
        }

        public Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}