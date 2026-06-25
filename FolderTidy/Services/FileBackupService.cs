using System.IO;
using FolderTidy.Models;

namespace FolderTidy.Services;

public static class FileBackupService
{
    public static string MoveToBackup(
        string sourcePath,
        string backupRoot,
        FileCategory category,
        bool createCategoryFolders)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("백업할 파일을 찾을 수 없습니다.", sourcePath);

        if (string.IsNullOrWhiteSpace(backupRoot))
            throw new InvalidOperationException("백업 폴더를 선택해 주세요.");

        if (!Directory.Exists(backupRoot))
            Directory.CreateDirectory(backupRoot);

        var targetDirectory = createCategoryFolders
            ? Path.Combine(backupRoot, category.GetDisplayName())
            : backupRoot;

        Directory.CreateDirectory(targetDirectory);

        var destinationPath = CreateUniqueFilePath(Path.Combine(targetDirectory, Path.GetFileName(sourcePath)));
        File.Move(sourcePath, destinationPath);
        return destinationPath;
    }

    private static string CreateUniqueFilePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName} ({index}){extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        throw new IOException($"같은 이름의 파일이 너무 많습니다: {path}");
    }
}
