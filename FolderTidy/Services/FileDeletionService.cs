using System.IO;

namespace FolderTidy.Services;

public static class FileDeletionService
{
    public static void DeletePermanently(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        File.SetAttributes(filePath, FileAttributes.Normal);
        File.Delete(filePath);
    }
}
