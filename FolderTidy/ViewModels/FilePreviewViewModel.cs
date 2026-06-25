namespace FolderTidy.ViewModels;

using System.Windows.Media;
using FolderTidy.Models;
using FolderTidy.Services;

public sealed class FilePreviewViewModel : ViewModelBase
{
    private int _loadVersion;
    private ImageSource? _previewImage;
    private string _previewText = string.Empty;
    private ArchiveTreeNodeViewModel? _archiveTreeRoot;
    private string _title = "미리보기";
    private string _description = "파일을 하나 선택하면 미리보기가 표시됩니다.";
    private string _hint = "PDF·압축·JSON·XML·TXT·이미지 등을 지원합니다.";
    private bool _isLoading;
    private bool _showImagePreview;
    private bool _showTextPreview;
    private bool _showTreePreview;

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    public string PreviewText
    {
        get => _previewText;
        private set => SetProperty(ref _previewText, value);
    }

    public ArchiveTreeNodeViewModel? ArchiveTreeRoot
    {
        get => _archiveTreeRoot;
        private set
        {
            if (SetProperty(ref _archiveTreeRoot, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(ShowEmptyPlaceholder));
            }
        }
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public string Hint
    {
        get => _hint;
        private set => SetProperty(ref _hint, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
                OnPropertyChanged(nameof(ShowEmptyPlaceholder));
        }
    }

    public bool ShowImagePreview
    {
        get => _showImagePreview;
        private set
        {
            if (SetProperty(ref _showImagePreview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(ShowEmptyPlaceholder));
            }
        }
    }

    public bool ShowTextPreview
    {
        get => _showTextPreview;
        private set
        {
            if (SetProperty(ref _showTextPreview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(ShowEmptyPlaceholder));
            }
        }
    }

    public bool ShowTreePreview
    {
        get => _showTreePreview;
        private set
        {
            if (SetProperty(ref _showTreePreview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(ShowEmptyPlaceholder));
            }
        }
    }

    public bool HasPreview => ShowImagePreview || ShowTextPreview || ShowTreePreview;

    public bool ShowEmptyPlaceholder => !IsLoading && !HasPreview;

    public async void LoadFrom(FileEntryViewModel? entry, int previewPixelSize)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        if (entry is null || entry.IsPendingDeletion)
        {
            Clear();
            return;
        }

        ResetPreviewState();
        IsLoading = true;
        Title = entry.Name;
        Description = $"{entry.CategoryName} · {entry.SizeText} · 생성 {entry.CreatedText}";

        var path = entry.Entry.FullPath;
        var previewKind = PreviewContentResolver.Resolve(entry.Entry);
        Hint = GetPreviewHint(entry.Entry, previewKind);

        try
        {
            switch (previewKind)
            {
                case PreviewContentKind.ArchiveTree:
                    await LoadArchiveTreeAsync(path, loadVersion);
                    break;

                case PreviewContentKind.Text:
                    await LoadTextPreviewAsync(path, entry.Entry.Extension, loadVersion);
                    break;

                case PreviewContentKind.Image:
                    await LoadImagePreviewAsync(path, entry.Entry, previewPixelSize, loadVersion);
                    break;

                default:
                    if (loadVersion == _loadVersion)
                        Hint = "이 파일 형식은 미리보기를 지원하지 않습니다.";
                    break;
            }
        }
        catch
        {
            if (loadVersion == _loadVersion)
            {
                ResetPreviewState();
                Hint = "미리보기를 불러오지 못했습니다.";
            }
        }
        finally
        {
            if (loadVersion == _loadVersion)
                IsLoading = false;
        }
    }

    public void Clear()
    {
        Interlocked.Increment(ref _loadVersion);
        ResetPreviewState();
        IsLoading = false;
        Title = "미리보기";
        Description = "파일을 하나 선택하면 미리보기가 표시됩니다.";
        Hint = "PDF·압축·JSON·XML·TXT·이미지 등을 지원합니다.";
    }

    private async Task LoadArchiveTreeAsync(string path, int loadVersion)
    {
        var cached = ArchivePreviewCache.TryGet(path);
        if (cached is not null)
        {
            if (loadVersion != _loadVersion)
                return;

            ArchiveTreeRoot = cached;
            ShowTreePreview = true;
            return;
        }

        var tree = await Task.Run(() => ArchivePreviewService.BuildTree(path));
        if (loadVersion != _loadVersion)
            return;

        if (tree is null)
        {
            Hint = "압축 파일 내용을 읽을 수 없습니다.";
            return;
        }

        ArchivePreviewCache.Set(path, tree);
        ArchiveTreeRoot = tree;
        ShowTreePreview = true;
    }

    private async Task LoadTextPreviewAsync(string path, string extension, int loadVersion)
    {
        var text = await TextPreviewService.LoadPrettyTextAsync(path, extension);
        if (loadVersion != _loadVersion)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            Hint = "텍스트 미리보기를 생성할 수 없습니다.";
            return;
        }

        PreviewText = text;
        ShowTextPreview = true;
    }

    private async Task LoadImagePreviewAsync(
        string path,
        FileEntry entry,
        int previewPixelSize,
        int loadVersion)
    {
        var extension = entry.Extension.ToLowerInvariant();
        ImageSource? image;

        if (extension == ".pdf")
        {
            image = await Task.Run(() => PdfPreviewService.RenderFirstPage(path, previewPixelSize));
            if (loadVersion != _loadVersion)
                return;

            if (image is not null)
            {
                PreviewImage = image;
                ShowImagePreview = true;
                Hint = "PDF 1페이지 미리보기 (Docnet / PDFium)";
                return;
            }

            Hint = "PDF 미리보기를 생성하지 못했습니다. Windows 썸네일을 시도합니다.";
        }

        image = await Task.Run(() =>
            ShellThumbnailService.GetLargePreview(path, previewPixelSize, entry.Category));

        if (loadVersion != _loadVersion)
            return;

        if (image is not null)
        {
            PreviewImage = image;
            ShowImagePreview = true;
            return;
        }

        if (entry.Category == FileCategory.Image)
        {
            image = await Task.Run(() =>
                ShellThumbnailService.GetLargePreview(path, previewPixelSize, FileCategory.Image));

            if (loadVersion != _loadVersion)
                return;

            if (image is not null)
            {
                PreviewImage = image;
                ShowImagePreview = true;
                return;
            }
        }

        Hint = $"{GetPreviewHint(entry, PreviewContentKind.Image)} Windows 미리보기를 생성하지 못했습니다.";
    }

    private void ResetPreviewState()
    {
        PreviewImage = null;
        PreviewText = string.Empty;
        ArchiveTreeRoot = null;
        ShowImagePreview = false;
        ShowTextPreview = false;
        ShowTreePreview = false;
    }

    private static string GetPreviewHint(FileEntry entry, PreviewContentKind kind)
    {
        var extension = entry.Extension.ToLowerInvariant();

        return kind switch
        {
            PreviewContentKind.ArchiveTree => "압축 파일 내용 (탐색기 트리)",
            PreviewContentKind.Text when extension == ".json" => "JSON 미리보기 (pretty print)",
            PreviewContentKind.Text when extension == ".xml" => "XML 미리보기 (pretty print)",
            PreviewContentKind.Text when extension == ".txt" => "텍스트 미리보기",
            PreviewContentKind.Text => "텍스트 미리보기",
            PreviewContentKind.Image when extension == ".pdf" => "PDF 미리보기",
            PreviewContentKind.Image when entry.Category == FileCategory.Image => "이미지 미리보기",
            PreviewContentKind.Image when entry.Category == FileCategory.GraphicDesign =>
                "그래픽 디자인 미리보기 (Windows 썸네일)",
            PreviewContentKind.Image when entry.Category == FileCategory.CadDrawing =>
                "CAD / 도면 미리보기 (Windows 썸네일)",
            PreviewContentKind.Image when entry.Category == FileCategory.Document =>
                "문서 미리보기 (Windows 썸네일)",
            PreviewContentKind.Image when entry.Category == FileCategory.Video => "동영상 썸네일",
            _ => "파일 정보"
        };
    }
}
