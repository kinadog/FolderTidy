namespace FolderTidy.Models;

public enum FileCategory
{
    Installer,
    Archive,
    Image,
    GraphicDesign,
    CadDrawing,
    Document,
    Video,
    Audio,
    Code,
    Other
}

public static class FileCategoryExtensions
{
    public static string GetDisplayName(this FileCategory category) => category switch
    {
        FileCategory.Installer => "설치 파일",
        FileCategory.Archive => "압축 파일",
        FileCategory.Image => "이미지",
        FileCategory.GraphicDesign => "그래픽 디자인",
        FileCategory.CadDrawing => "CAD / 도면",
        FileCategory.Document => "문서",
        FileCategory.Video => "동영상",
        FileCategory.Audio => "오디오",
        FileCategory.Code => "코드",
        _ => "기타"
    };

    public static int GetSortOrder(this FileCategory category) => category switch
    {
        FileCategory.Installer => 0,
        FileCategory.Archive => 1,
        FileCategory.Image => 2,
        FileCategory.GraphicDesign => 3,
        FileCategory.CadDrawing => 4,
        FileCategory.Document => 5,
        FileCategory.Video => 6,
        FileCategory.Audio => 7,
        FileCategory.Code => 8,
        _ => 99
    };
}
