namespace FolderTidy.Models;

public enum ThumbnailSize
{
    None,
    Small,
    Medium,
    Large,
    ExtraLarge
}

public static class ThumbnailSizeExtensions
{
    public static int GetPixelSize(this ThumbnailSize size) => size switch
    {
        ThumbnailSize.None => 0,
        ThumbnailSize.Small => 64,
        ThumbnailSize.Medium => 96,
        ThumbnailSize.Large => 144,
        ThumbnailSize.ExtraLarge => 192,
        _ => 96
    };

    public static string GetDisplayName(this ThumbnailSize size) => size switch
    {
        ThumbnailSize.None => "목록만 (썸네일 없음)",
        ThumbnailSize.Small => "작게 (64px)",
        ThumbnailSize.Medium => "보통 (96px)",
        ThumbnailSize.Large => "크게 (144px)",
        ThumbnailSize.ExtraLarge => "매우 크게 (192px)",
        _ => size.ToString()
    };
}
