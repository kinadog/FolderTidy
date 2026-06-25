namespace FolderTidy.ViewModels;

using System.Collections.ObjectModel;
using FolderTidy.Helpers;
using FolderTidy.Models;

public sealed class FileGroupViewModel : ViewModelBase
{
    private double _occupancyPercent;
    private string _occupancyPercentText = "0%";
    private string _totalSizeText;

    public FileGroupViewModel(FileGroupModel model, int thumbnailPixelSize, long totalDirectoryBytes)
    {
        Category = model.Category;
        CategoryName = model.Category.GetDisplayName();
        FileCount = model.FileCount;
        TotalSizeBytes = model.TotalSizeBytes;
        LatestCreatedAt = model.LatestCreatedAt;
        FileCountText = $"{model.FileCount:N0}개";
        _totalSizeText = FileSizeFormatter.Format(model.TotalSizeBytes);
        DateRangeText = $"{model.EarliestCreatedAt:yyyy-MM-dd} ~ {model.LatestCreatedAt:yyyy-MM-dd}";
        var showSectionHeaders = model.SubGroups.Count > 1
            || (model.SubGroups.Count == 1 && model.SubGroups[0].Label != "전체");
        SubGroups = new ObservableCollection<SubGroupViewModel>(
            model.SubGroups.Select(sg => new SubGroupViewModel(sg, thumbnailPixelSize, showSectionHeaders, totalDirectoryBytes)));

        foreach (var subGroup in SubGroups)
        {
            foreach (var file in subGroup.Files)
                file.ParentGroup = this;
        }

        UpdateOccupancy(totalDirectoryBytes);
    }

    public FileCategory Category { get; }
    public string CategoryName { get; }
    public int FileCount { get; }
    public long TotalSizeBytes { get; }
    public DateTime LatestCreatedAt { get; }
    public string FileCountText { get; }

    public string TotalSizeText
    {
        get => _totalSizeText;
        private set => SetProperty(ref _totalSizeText, value);
    }

    public string DateRangeText { get; }
    public ObservableCollection<SubGroupViewModel> SubGroups { get; }

    public double OccupancyPercent
    {
        get => _occupancyPercent;
        private set => SetProperty(ref _occupancyPercent, value);
    }

    public string OccupancyPercentText
    {
        get => _occupancyPercentText;
        private set => SetProperty(ref _occupancyPercentText, value);
    }

    public IEnumerable<FileEntryViewModel> AllFiles => SubGroups.SelectMany(sg => sg.Files);

    public IEnumerable<FileEntryViewModel> ActiveFiles =>
        AllFiles.Where(file => file.IsActiveInList);

    public event Action? SelectionStateChanged;

    public void NotifySelectionStateChanged() => SelectionStateChanged?.Invoke();

    public void LoadThumbnails()
    {
        foreach (var file in AllFiles)
            file.LoadPreviewIfNeeded();
    }

    public void UpdateOccupancy(long totalDirectoryBytes)
    {
        var groupBytes = AllFiles.Sum(file => file.Entry.SizeBytes);
        TotalSizeText = FileSizeFormatter.Format(groupBytes);
        OccupancyShare.Apply(groupBytes, totalDirectoryBytes, out var percent, out var percentText);
        OccupancyPercent = percent;
        OccupancyPercentText = percentText;

        foreach (var subGroup in SubGroups)
            subGroup.UpdateOccupancy(totalDirectoryBytes);
    }
}
