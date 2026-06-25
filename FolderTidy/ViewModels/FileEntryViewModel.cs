namespace FolderTidy.ViewModels;

using System.Windows.Media;
using FolderTidy.Helpers;
using FolderTidy.Models;
using FolderTidy.Services;

public sealed class FileEntryViewModel : ViewModelBase
{
    private ImageSource? _preview;
    private bool _isSelected;
    private bool _isPendingDeletion;
    private bool _isPendingBackup;
    private bool _previewLoadStarted;
    private int _thumbnailPixelSize;
    private double _occupancyPercent;
    private string _occupancyPercentText = "0%";

    public FileEntryViewModel(FileEntry entry, int thumbnailPixelSize, long totalDirectoryBytes)
    {
        Entry = entry;
        _thumbnailPixelSize = thumbnailPixelSize;
        Name = entry.Name;
        Extension = string.IsNullOrWhiteSpace(entry.Extension) ? "(확장자 없음)" : entry.Extension.ToUpperInvariant();
        SizeText = FileSizeFormatter.Format(entry.SizeBytes);
        CreatedText = entry.CreatedAt.ToString("yyyy-MM-dd");
        ModifiedText = entry.ModifiedAt.ToString("yyyy-MM-dd");
        LocationText = string.IsNullOrWhiteSpace(entry.RelativePath) || entry.RelativePath == "."
            ? string.Empty
            : entry.RelativePath;
        InfoText = BuildInfoText(entry);
        CategoryName = entry.Category.GetDisplayName();
        FileIcon = ShellIconService.GetFileIcon(entry.Extension);
        UpdateOccupancy(entry.SizeBytes, totalDirectoryBytes);
    }

    public FileEntry Entry { get; }
    public FileGroupViewModel? ParentGroup { get; set; }

    public string Name { get; }
    public string Extension { get; }
    public string SizeText { get; }
    public string CreatedText { get; }
    public string ModifiedText { get; }
    public string LocationText { get; }
    public string InfoText { get; }
    public string CategoryName { get; }
    public ImageSource FileIcon { get; }

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

    public bool ShowListThumbnail =>
        Entry.Category == FileCategory.Image && _thumbnailPixelSize > 0;

    public bool ShowFileIcon => !ShowListThumbnail;

    public int ThumbnailPixelSize
    {
        get => _thumbnailPixelSize;
        set
        {
            if (!SetProperty(ref _thumbnailPixelSize, value))
                return;

            OnPropertyChanged(nameof(ShowListThumbnail));
            OnPropertyChanged(nameof(ShowFileIcon));

            if (!ShowListThumbnail)
            {
                Preview = null;
                _previewLoadStarted = false;
                return;
            }

            if (_previewLoadStarted)
                LoadPreviewAsync(force: true);
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsPendingDeletion
    {
        get => _isPendingDeletion;
        set
        {
            if (!SetProperty(ref _isPendingDeletion, value))
                return;

            NotifyPendingStateChanged();
        }
    }

    public bool IsPendingBackup
    {
        get => _isPendingBackup;
        set
        {
            if (!SetProperty(ref _isPendingBackup, value))
                return;

            NotifyPendingStateChanged();
        }
    }

    public bool IsDimmedInList => IsPendingDeletion || IsPendingBackup;

    public bool IsActiveInList => !IsPendingDeletion && !IsPendingBackup;

    public string StatusLabel => IsPendingDeletion
        ? "삭제 예정"
        : IsPendingBackup
            ? "백업 예정"
            : string.Empty;

    public ImageSource? Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    public void UpdateOccupancy(long sizeBytes, long totalDirectoryBytes)
    {
        OccupancyShare.Apply(sizeBytes, totalDirectoryBytes, out var percent, out var percentText);
        OccupancyPercent = percent;
        OccupancyPercentText = percentText;
    }

    public void LoadPreviewIfNeeded()
    {
        if (!ShowListThumbnail || _previewLoadStarted)
            return;

        _previewLoadStarted = true;
        LoadPreviewAsync(force: false);
    }

    private void NotifyPendingStateChanged()
    {
        OnPropertyChanged(nameof(IsDimmedInList));
        OnPropertyChanged(nameof(IsActiveInList));
        OnPropertyChanged(nameof(StatusLabel));
    }

    private static string BuildInfoText(FileEntry entry) => entry.Category switch
    {
        FileCategory.Installer => "실행/설치",
        FileCategory.Archive => "압축",
        FileCategory.Image => "이미지",
        FileCategory.GraphicDesign => "그래픽 디자인",
        FileCategory.CadDrawing => "CAD / 도면",
        FileCategory.Document => "문서",
        FileCategory.Video => "동영상",
        FileCategory.Audio => "오디오",
        FileCategory.Code => "코드",
        _ => "기타"
    };

    private async void LoadPreviewAsync(bool force)
    {
        if (!ShowListThumbnail)
            return;

        if (!force && Preview is not null)
            return;

        try
        {
            var path = Entry.FullPath;
            var pixelSize = ThumbnailPixelSize;

            var bitmap = await Task.Run(() =>
                ShellThumbnailService.GetThumbnail(path, pixelSize, FileCategory.Image));

            Preview = bitmap;
        }
        catch
        {
            Preview = null;
        }
    }
}
