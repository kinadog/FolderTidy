namespace FolderTidy.Models;

public sealed class FileGroupModel
{
    public required FileCategory Category { get; init; }
    public required IReadOnlyList<FileEntry> Files { get; init; }
    public required IReadOnlyList<SubGroupModel> SubGroups { get; init; }

    public int FileCount => Files.Count;
    public long TotalSizeBytes => Files.Sum(f => f.SizeBytes);
    public DateTime EarliestCreatedAt => Files.Min(f => f.CreatedAt);
    public DateTime LatestCreatedAt => Files.Max(f => f.CreatedAt);
}

public sealed class SubGroupModel
{
    public required string Label { get; init; }
    public required IReadOnlyList<FileEntry> Files { get; init; }

    public int FileCount => Files.Count;
    public long TotalSizeBytes => Files.Sum(f => f.SizeBytes);
}
