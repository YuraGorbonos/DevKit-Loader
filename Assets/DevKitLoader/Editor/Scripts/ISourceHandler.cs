using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevKitLoader
{
    public interface ISourceHandler
    {
        /// <summary>
        /// Async installation of the tool.
        /// </summary>
        /// <param name="onProgress">Progress updates: (message, progress01)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task InstallAsync(Action<string, float> onProgress, CancellationToken cancellationToken);
    }
}