namespace FolderTidy.Models;

public enum GroupSortMode
{
    TotalSize,
    FileCount,
    LatestCreatedDate
}

public static class GroupSortModeExtensions
{
    public static string GetDisplayName(this GroupSortMode mode) => mode switch
    {
        GroupSortMode.TotalSize => "총 용량 (큰 순)",
        GroupSortMode.FileCount => "파일 수 (많은 순)",
        GroupSortMode.LatestCreatedDate => "최근 생성일",
        _ => mode.ToString()
    };
}
