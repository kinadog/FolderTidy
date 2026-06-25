namespace FolderTidy.Services;

using FolderTidy.Models;

public static class SubGroupBuilder
{
    public static IReadOnlyList<SubGroupModel> Build(IReadOnlyList<FileEntry> files, SubGroupMode mode)
    {
        if (mode == SubGroupMode.None || files.Count == 0)
        {
            return
            [
                new SubGroupModel
                {
                    Label = "전체",
                    Files = files
                }
            ];
        }

        IEnumerable<IGrouping<string, FileEntry>> grouped = mode switch
        {
            SubGroupMode.ByCreationDate => files.GroupBy(f => FormatMonth(f.CreatedAt)),
            SubGroupMode.ByLastModifiedDate => files.GroupBy(f => FormatMonth(f.ModifiedAt)),
            SubGroupMode.ByFileSize => files.GroupBy(f => GetSizeBucket(f.SizeBytes)),
            _ => files.GroupBy(_ => "전체")
        };

        return grouped
            .OrderBy(g => GetSubGroupSortKey(g.Key, mode))
            .Select(g => new SubGroupModel
            {
                Label = g.Key,
                Files = g.OrderByDescending(f => f.SizeBytes).ToList()
            })
            .ToList();
    }

    private static string FormatMonth(DateTime date) => $"{date:yyyy년 MM월}";

    private static string GetSizeBucket(long bytes) => bytes switch
    {
        < 1_048_576 => "소형 (1MB 미만)",
        < 104_857_600 => "중형 (1MB ~ 100MB)",
        < 1_073_741_824 => "대형 (100MB ~ 1GB)",
        _ => "초대형 (1GB 이상)"
    };

    private static string GetSubGroupSortKey(string label, SubGroupMode mode)
    {
        if (mode == SubGroupMode.ByFileSize)
        {
            return label switch
            {
                "소형 (1MB 미만)" => "0",
                "중형 (1MB ~ 100MB)" => "1",
                "대형 (100MB ~ 1GB)" => "2",
                _ => "3"
            };
        }

        return label;
    }
}
