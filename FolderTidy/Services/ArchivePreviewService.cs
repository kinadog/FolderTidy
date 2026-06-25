namespace FolderTidy.Services;

using System.IO;
using FolderTidy.Helpers;
using FolderTidy.ViewModels;
using SharpCompress.Archives;

public static class ArchivePreviewService
{
    private const int MaxEntries = 1000;

    public static ArchiveTreeNodeViewModel? BuildTree(string archivePath)
    {
        if (!File.Exists(archivePath))
            return null;

        try
        {
            using var archive = ArchiveFactory.OpenArchive(archivePath);
            var root = new ArchiveTreeNodeViewModel
            {
                Name = Path.GetFileName(archivePath),
                Detail = FileSizeFormatter.Format(new FileInfo(archivePath).Length),
                IsDirectory = true,
                Icon = ShellIconService.GetArchiveFileIcon(archivePath)
            };

            var directoryNodes = new Dictionary<string, ArchiveTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = root
            };

            var entries = archive.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Take(MaxEntries)
                .ToList();

            foreach (var entry in entries)
            {
                var normalizedPath = entry.Key!.Replace('\\', '/').Trim('/');
                if (string.IsNullOrWhiteSpace(normalizedPath))
                    continue;

                var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var parent = root;
                var currentPath = string.Empty;

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var isLast = i == parts.Length - 1;
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                    if (isLast && !entry.IsDirectory)
                    {
                        parent.Children.Add(new ArchiveTreeNodeViewModel
                        {
                            Name = part,
                            Detail = FileSizeFormatter.Format(entry.Size),
                            IsDirectory = false,
                            Icon = ShellIconService.GetFileIcon(Path.GetExtension(part))
                        });
                        break;
                    }

                    if (!directoryNodes.TryGetValue(currentPath, out var directoryNode))
                    {
                        directoryNode = new ArchiveTreeNodeViewModel
                        {
                            Name = part,
                            Detail = string.Empty,
                            IsDirectory = true,
                            Icon = ShellIconService.GetFolderIcon()
                        };
                        parent.Children.Add(directoryNode);
                        directoryNodes[currentPath] = directoryNode;
                    }

                    parent = directoryNode;
                }
            }

            SortTree(root);

            if (entries.Count >= MaxEntries)
            {
                root.Children.Add(new ArchiveTreeNodeViewModel
                {
                    Name = $"... 항목 {MaxEntries:N0}개까지만 표시",
                    Detail = string.Empty,
                    IsPlaceholder = true,
                    Icon = ShellIconService.GetFileIcon(".txt")
                });
            }

            return root;
        }
        catch
        {
            return null;
        }
    }

    private static void SortTree(ArchiveTreeNodeViewModel node)
    {
        var ordered = node.Children
            .OrderByDescending(child => child.IsDirectory && !child.IsPlaceholder)
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        node.Children.Clear();
        foreach (var child in ordered)
        {
            SortTree(child);
            node.Children.Add(child);
        }
    }
}
