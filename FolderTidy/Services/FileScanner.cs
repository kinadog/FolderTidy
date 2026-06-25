namespace FolderTidy.Services;

using System.IO;
using FolderTidy.Models;

public static class FileScanner
{
    public static IReadOnlyList<FileEntry> Scan(string rootPath, bool includeSubfolders)
    {
        if (!Directory.Exists(rootPath))
            return [];

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var entries = new List<FileEntry>();

        foreach (var path in Directory.EnumerateFiles(rootPath, "*", searchOption))
        {
            try
            {
                var info = new FileInfo(path);
                var extension = info.Extension;

                entries.Add(new FileEntry
                {
                    FullPath = info.FullName,
                    Name = info.Name,
                    Extension = extension,
                    Category = FileCategoryClassifier.Classify(extension),
                    SizeBytes = info.Length,
                    CreatedAt = info.CreationTime,
                    ModifiedAt = info.LastWriteTime,
                    RelativePath = includeSubfolders
                        ? Path.GetRelativePath(rootPath, info.DirectoryName ?? rootPath)
                        : null
                });
            }
            catch (IOException)
            {
                // Skip files that cannot be accessed.
            }
            catch (UnauthorizedAccessException)
            {
                // Skip files that cannot be accessed.
            }
        }

        return entries;
    }

    public static IReadOnlyList<FileGroupModel> GroupFiles(
        IReadOnlyList<FileEntry> files,
        SubGroupMode subGroupMode)
    {
        return files
            .GroupBy(f => f.Category)
            .OrderBy(g => g.Key.GetSortOrder())
            .Select(g =>
            {
                var orderedFiles = g.OrderByDescending(f => f.SizeBytes).ToList();
                return new FileGroupModel
                {
                    Category = g.Key,
                    Files = orderedFiles,
                    SubGroups = SubGroupBuilder.Build(orderedFiles, subGroupMode)
                };
            })
            .ToList();
    }
}
