namespace FolderTidy.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Media;

public sealed class ArchiveTreeNodeViewModel
{
    public required string Name { get; init; }
    public string Detail { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public bool IsPlaceholder { get; init; }
    public ImageSource? Icon { get; init; }
    public ObservableCollection<ArchiveTreeNodeViewModel> Children { get; } = [];

    public string SizeDisplay => IsDirectory || IsPlaceholder ? string.Empty : Detail;
}
