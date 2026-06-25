namespace FolderTidy.Models;

public sealed class FileEntry
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required FileCategory Category { get; init; }
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public string? RelativePath { get; init; }
}
