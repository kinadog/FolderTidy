namespace FolderTidy.ViewModels;

using System.Collections.ObjectModel;
using FolderTidy.Helpers;
using FolderTidy.Models;

public sealed class SubGroupViewModel : ViewModelBase
{
    private double _occupancyPercent;
    private string _occupancyPercentText = "0%";
    private string _totalSizeText;

    public SubGroupViewModel(SubGroupModel model, int thumbnailPixelSize, bool showSectionHeader, long totalDirectoryBytes)
    {
        Label = model.Label;
        FileCountText = $"{model.FileCount:N0}개";
        _totalSizeText = FileSizeFormatter.Format(model.TotalSizeBytes);
        ShowSectionHeader = showSectionHeader;
        Files = new ObservableCollection<FileEntryViewModel>(
            model.Files.Select(f => new FileEntryViewModel(f, thumbnailPixelSize, totalDirectoryBytes)));
        UpdateOccupancy(totalDirectoryBytes);
    }

    public string Label { get; }
    public string FileCountText { get; }

    public string TotalSizeText
    {
        get => _totalSizeText;
        private set => SetProperty(ref _totalSizeText, value);
    }

    public bool ShowSectionHeader { get; }
    public ObservableCollection<FileEntryViewModel> Files { get; }

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

    public void UpdateOccupancy(long totalDirectoryBytes)
    {
        var subGroupBytes = Files.Sum(file => file.Entry.SizeBytes);
        TotalSizeText = FileSizeFormatter.Format(subGroupBytes);
        OccupancyShare.Apply(subGroupBytes, totalDirectoryBytes, out var percent, out var percentText);
        OccupancyPercent = percent;
        OccupancyPercentText = percentText;
    }
}
