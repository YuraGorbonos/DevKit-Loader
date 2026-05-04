using System;

namespace DevKitLoader
{
    public static class SourceHandlerFactory
    {
        public static ISourceHandler CreateHandler(ToolEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return entry.Type switch
            {
                SourceType.GitHubRelease => new GitHubReleaseHandler(entry),
                SourceType.GitLabRelease => new GitLabReleaseHandler(entry),
                SourceType.DirectUrl => new DirectUrlHandler(entry),
                SourceType.GitUpm => new UpmHandler(entry),
                SourceType.AssetStore => new AssetStoreHandler(entry),
                _ => throw new NotSupportedException($"Unsupported source type: {entry.Type}")
            };
        }
    }
}
