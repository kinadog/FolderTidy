namespace FolderTidy.Services;

using FolderTidy.ViewModels;

public static class ArchivePreviewCache
{
    private static readonly Dictionary<string, ArchiveTreeNodeViewModel> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ArchiveTreeNodeViewModel? TryGet(string archivePath)
    {
        lock (Cache)
            return Cache.TryGetValue(archivePath, out var tree) ? tree : null;
    }

    public static void Set(string archivePath, ArchiveTreeNodeViewModel tree)
    {
        lock (Cache)
            Cache[archivePath] = tree;
    }

    public static void Remove(string archivePath)
    {
        lock (Cache)
            Cache.Remove(archivePath);
    }

    public static void Clear()
    {
        lock (Cache)
            Cache.Clear();
    }
}
