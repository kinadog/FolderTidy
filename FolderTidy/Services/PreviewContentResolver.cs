namespace FolderTidy.Services;

using FolderTidy.Models;

public static class PreviewContentResolver
{
    public static PreviewContentKind Resolve(FileEntry entry)
    {
        var extension = NormalizeExtension(entry.Extension);

        if (extension == ".pdf")
            return PreviewContentKind.Image;

        if (entry.Category == FileCategory.Archive)
            return PreviewContentKind.ArchiveTree;

        if (TextPreviewService.SupportsExtension(extension))
            return PreviewContentKind.Text;

        if (entry.Category is FileCategory.Image
            or FileCategory.Document
            or FileCategory.Video
            or FileCategory.GraphicDesign
            or FileCategory.CadDrawing)
        {
            return PreviewContentKind.Image;
        }

        return PreviewContentKind.None;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }
}
