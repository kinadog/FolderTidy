namespace FolderTidy.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FolderTidy.Helpers;
using FolderTidy.Models;
using FolderTidy.Services;
using Microsoft.Win32;

public sealed class MainViewModel : ViewModelBase
{
    private readonly HashSet<string> _pendingDeletionPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pendingBackupPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<FileEntryViewModel> _selectedFiles = [];

    private string _selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string _backupDestinationPath = string.Empty;
    private bool _createCategoryFoldersOnBackup;
    private bool _includeSubfolders;
    private SubGroupMode _subGroupMode = SubGroupMode.None;
    private GroupSortMode _groupSortMode = GroupSortMode.TotalSize;
    private ThumbnailSize _thumbnailSize = ThumbnailSize.Medium;
    private SubGroupOptionViewModel _selectedSubGroupOption;
    private GroupSortOptionViewModel _selectedGroupSortOption;
    private ThumbnailSizeOptionViewModel _selectedThumbnailSizeOption;
    private string _statusText = "폴더를 선택한 뒤 [불러오기]를 눌러 주세요.";
    private string _selectionText = string.Empty;
    private string _pendingDeletionTabHeader = "삭제 예정 (0)";
    private string _pendingBackupTabHeader = "백업 예정 (0)";
    private bool _isBusy;
    private FileEntryViewModel? _selectionAnchor;

    public MainViewModel()
    {
        BrowseCommand = new RelayCommand(BrowseFolder);
        RefreshCommand = new RelayCommand(Refresh, () => !IsBusy && Directory.Exists(SelectedPath));
        OpenFileCommand = new RelayCommand(OpenFile, CanInteractWithFile);
        MarkForDeletionCommand = new RelayCommand(MarkForDeletion, CanMarkForDeletion);
        MarkForBackupCommand = new RelayCommand(MarkForBackup, CanMarkForBackup);
        SelectAllInGroupCommand = new RelayCommand(SelectAllInGroup);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        RestoreFromPendingCommand = new RelayCommand(
            RestoreFromPending,
            CanRestoreFromPending);
        RestoreFromPendingBackupCommand = new RelayCommand(
            RestoreFromPendingBackup,
            CanRestoreFromPendingBackup);
        PermanentlyDeletePendingCommand = new RelayCommand(
            () => PermanentlyDeletePending(null),
            () => PendingDeletionFiles.Count > 0);
        ExecuteBackupPendingCommand = new RelayCommand(
            () => ExecuteBackupPending(null),
            () => CanExecuteBackup(null));
        BrowseBackupFolderCommand = new RelayCommand(BrowseBackupFolder);
        OpenFolderCommand = new RelayCommand(OpenContainingFolder, () => !string.IsNullOrWhiteSpace(SelectedPath));

        SubGroupOptions =
        [
            new SubGroupOptionViewModel(SubGroupMode.None, "세부 그룹 없음"),
            new SubGroupOptionViewModel(SubGroupMode.ByCreationDate, "생성일 (월별)"),
            new SubGroupOptionViewModel(SubGroupMode.ByLastModifiedDate, "수정일 (월별)"),
            new SubGroupOptionViewModel(SubGroupMode.ByFileSize, "파일 크기")
        ];

        _selectedSubGroupOption = SubGroupOptions[0];

        GroupSortOptions =
        [
            new GroupSortOptionViewModel(GroupSortMode.TotalSize, GroupSortMode.TotalSize.GetDisplayName()),
            new GroupSortOptionViewModel(GroupSortMode.FileCount, GroupSortMode.FileCount.GetDisplayName()),
            new GroupSortOptionViewModel(GroupSortMode.LatestCreatedDate, GroupSortMode.LatestCreatedDate.GetDisplayName())
        ];
        _selectedGroupSortOption = GroupSortOptions[0];

        ThumbnailSizeOptions =
        [
            new ThumbnailSizeOptionViewModel(ThumbnailSize.None, ThumbnailSize.None.GetDisplayName()),
            new ThumbnailSizeOptionViewModel(ThumbnailSize.Small, ThumbnailSize.Small.GetDisplayName()),
            new ThumbnailSizeOptionViewModel(ThumbnailSize.Medium, ThumbnailSize.Medium.GetDisplayName()),
            new ThumbnailSizeOptionViewModel(ThumbnailSize.Large, ThumbnailSize.Large.GetDisplayName()),
            new ThumbnailSizeOptionViewModel(ThumbnailSize.ExtraLarge, ThumbnailSize.ExtraLarge.GetDisplayName())
        ];
        _selectedThumbnailSizeOption = ThumbnailSizeOptions[2];

        FilePreview = new FilePreviewViewModel();
    }

    public ObservableCollection<FileGroupViewModel> FileGroups { get; } = [];
    public ObservableCollection<FileEntryViewModel> PendingDeletionFiles { get; } = [];
    public ObservableCollection<FileEntryViewModel> PendingBackupFiles { get; } = [];
    public ObservableCollection<SubGroupOptionViewModel> SubGroupOptions { get; }
    public ObservableCollection<GroupSortOptionViewModel> GroupSortOptions { get; }
    public ObservableCollection<ThumbnailSizeOptionViewModel> ThumbnailSizeOptions { get; }
    public FilePreviewViewModel FilePreview { get; }

    public int ThumbnailPixelSize => _thumbnailSize.GetPixelSize();
    public int PreviewPixelSize => Math.Max(ThumbnailPixelSize > 0 ? ThumbnailPixelSize * 2 : 256, 256);

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (SetProperty(ref _selectedPath, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set
        {
            if (SetProperty(ref _includeSubfolders, value))
                Refresh();
        }
    }

    public SubGroupOptionViewModel SelectedSubGroupOption
    {
        get => _selectedSubGroupOption;
        set
        {
            if (value is null || ReferenceEquals(_selectedSubGroupOption, value))
                return;

            _selectedSubGroupOption = value;
            _subGroupMode = value.Mode;
            OnPropertyChanged();
            Refresh();
        }
    }

    public GroupSortOptionViewModel SelectedGroupSortOption
    {
        get => _selectedGroupSortOption;
        set
        {
            if (value is null || ReferenceEquals(_selectedGroupSortOption, value))
                return;

            _selectedGroupSortOption = value;
            _groupSortMode = value.Mode;
            OnPropertyChanged();
            ApplyGroupSort();
        }
    }

    public ThumbnailSizeOptionViewModel SelectedThumbnailSizeOption
    {
        get => _selectedThumbnailSizeOption;
        set
        {
            if (value is null || ReferenceEquals(_selectedThumbnailSizeOption, value))
                return;

            _selectedThumbnailSizeOption = value;
            _thumbnailSize = value.Size;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThumbnailPixelSize));
            OnPropertyChanged(nameof(PreviewPixelSize));
            ApplyThumbnailSize();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SelectionText
    {
        get => _selectionText;
        private set => SetProperty(ref _selectionText, value);
    }

    public string PendingDeletionTabHeader
    {
        get => _pendingDeletionTabHeader;
        private set => SetProperty(ref _pendingDeletionTabHeader, value);
    }

    public string PendingBackupTabHeader
    {
        get => _pendingBackupTabHeader;
        private set => SetProperty(ref _pendingBackupTabHeader, value);
    }

    public string BackupDestinationPath
    {
        get => _backupDestinationPath;
        set
        {
            if (SetProperty(ref _backupDestinationPath, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool CreateCategoryFoldersOnBackup
    {
        get => _createCategoryFoldersOnBackup;
        set => SetProperty(ref _createCategoryFoldersOnBackup, value);
    }

    public event Action? NavigateToBackupTabRequested;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public ICommand BrowseCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand MarkForDeletionCommand { get; }
    public ICommand MarkForBackupCommand { get; }
    public ICommand SelectAllInGroupCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand RestoreFromPendingCommand { get; }
    public ICommand RestoreFromPendingBackupCommand { get; }
    public ICommand PermanentlyDeletePendingCommand { get; }
    public ICommand ExecuteBackupPendingCommand { get; }
    public ICommand BrowseBackupFolderCommand { get; }
    public ICommand OpenFolderCommand { get; }

    public void HandleFileSelection(
        FileEntryViewModel entry,
        ModifierKeys modifiers)
    {
        var group = entry.ParentGroup;
        if (group is null)
            return;

        var selectableFiles = group.AllFiles.ToList();
        if (selectableFiles.Count == 0)
            return;

        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift &&
            _selectionAnchor is not null &&
            ReferenceEquals(_selectionAnchor.ParentGroup, group))
        {
            SelectRange(selectableFiles, _selectionAnchor, entry);
        }
        else if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ToggleSelection(entry);
            _selectionAnchor = entry;
        }
        else
        {
            ClearSelection();
            SetSelected(entry, true);
            _selectionAnchor = entry;
        }

        UpdateSelectionText();
        UpdateFilePreview();
    }

    public void MarkSelectedForDeletion()
    {
        var targets = GetSelectedActiveFiles();
        if (targets.Count == 0)
            return;

        MarkFilesForDeletion(targets);
    }

    public void MarkSelectedForBackup()
    {
        var targets = GetSelectedActiveFiles();
        if (targets.Count == 0)
            return;

        MarkFilesForBackup(targets);
    }

    public void PermanentlyDeletePending(IReadOnlyList<FileEntryViewModel>? explicitTargets)
    {
        var targets = explicitTargets?.Where(f => f.IsPendingDeletion).ToList()
                      ?? PendingDeletionFiles.ToList();

        if (targets.Count == 0)
            return;

        var totalSize = targets.Sum(f => f.Entry.SizeBytes);
        var message =
            $"{targets.Count:N0}개 파일을 영구 삭제합니다.\n" +
            $"총 {FileSizeFormatter.Format(totalSize)}\n\n" +
            "휴지통으로 이동하지 않으며 복구할 수 없습니다. 계속하시겠습니까?";

        if (MessageBox.Show(message, "완전 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var failures = new List<string>();

        foreach (var file in targets.ToList())
        {
            try
            {
                FileDeletionService.DeletePermanently(file.Entry.FullPath);
                _pendingDeletionPaths.Remove(file.Entry.FullPath);
                ArchivePreviewCache.Remove(file.Entry.FullPath);
                RemoveFileFromMainList(file);
                PendingDeletionFiles.Remove(file);
            }
            catch (Exception ex)
            {
                failures.Add($"{file.Name}: {ex.Message}");
            }
        }

        ClearSelection();
        RecalculateOccupancyShares();
        UpdatePendingTabHeader();
        UpdateStatusSummary();

        if (failures.Count > 0)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, failures),
                "일부 파일 삭제 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void RestorePendingItems(IReadOnlyList<FileEntryViewModel> targets)
    {
        foreach (var file in targets.ToList())
        {
            file.IsPendingDeletion = false;
            _pendingDeletionPaths.Remove(file.Entry.FullPath);
            PendingDeletionFiles.Remove(file);
        }

        UpdatePendingTabHeader();
        UpdateStatusSummary();
        CommandManager.InvalidateRequerySuggested();
    }

    public void RestorePendingBackupItems(IReadOnlyList<FileEntryViewModel> targets)
    {
        foreach (var file in targets.ToList())
        {
            file.IsPendingBackup = false;
            _pendingBackupPaths.Remove(file.Entry.FullPath);
            PendingBackupFiles.Remove(file);
        }

        UpdatePendingBackupTabHeader();
        UpdateStatusSummary();
        CommandManager.InvalidateRequerySuggested();
    }

    public void ExecuteBackupPending(IReadOnlyList<FileEntryViewModel>? explicitTargets)
    {
        if (!CanExecuteBackup(explicitTargets?.FirstOrDefault()))
        {
            MessageBox.Show(
                "백업 폴더를 선택해 주세요.",
                "FolderTidy",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var targets = explicitTargets?.Where(f => f.IsPendingBackup).ToList()
                      ?? PendingBackupFiles.ToList();

        if (targets.Count == 0)
            return;

        var totalSize = targets.Sum(f => f.Entry.SizeBytes);
        var folderOptionText = CreateCategoryFoldersOnBackup
            ? "파일 종류별 하위 폴더를 만들어 이동합니다."
            : "선택한 백업 폴더로 바로 이동합니다.";
        var message =
            $"{targets.Count:N0}개 파일을 백업 폴더로 이동합니다.\n" +
            $"대상: {BackupDestinationPath}\n" +
            $"총 {FileSizeFormatter.Format(totalSize)}\n" +
            $"{folderOptionText}\n\n" +
            "원본 위치에서는 파일이 제거됩니다. 계속하시겠습니까?";

        if (MessageBox.Show(message, "백업 실행 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var failures = new List<string>();

        foreach (var file in targets.ToList())
        {
            try
            {
                FileBackupService.MoveToBackup(
                    file.Entry.FullPath,
                    BackupDestinationPath,
                    file.Entry.Category,
                    CreateCategoryFoldersOnBackup);

                _pendingBackupPaths.Remove(file.Entry.FullPath);
                ArchivePreviewCache.Remove(file.Entry.FullPath);
                RemoveFileFromMainList(file);
                PendingBackupFiles.Remove(file);
            }
            catch (Exception ex)
            {
                failures.Add($"{file.Name}: {ex.Message}");
            }
        }

        ClearSelection();
        RecalculateOccupancyShares();
        UpdatePendingBackupTabHeader();
        UpdateStatusSummary();

        if (failures.Count > 0)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, failures),
                "일부 파일 백업 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BrowseBackupFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "백업 폴더 선택",
            InitialDirectory = Directory.Exists(BackupDestinationPath)
                ? BackupDestinationPath
                : Directory.Exists(SelectedPath) ? SelectedPath : null
        };

        if (dialog.ShowDialog() == true)
            BackupDestinationPath = dialog.FolderName;
    }

    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "정리할 폴더 선택",
            InitialDirectory = Directory.Exists(SelectedPath) ? SelectedPath : null
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedPath = dialog.FolderName;
            Refresh();
        }
    }

    private async void Refresh()
    {
        if (IsBusy || !Directory.Exists(SelectedPath))
            return;

        IsBusy = true;
        StatusText = "파일을 검색하는 중...";

        try
        {
            var path = SelectedPath;
            var includeSubfolders = IncludeSubfolders;
            var subGroupMode = _subGroupMode;
            var thumbnailPixelSize = ThumbnailPixelSize;
            var pendingDeletionPaths = _pendingDeletionPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var pendingBackupPaths = _pendingBackupPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var builtGroups = await Task.Run(() =>
            {
                var files = FileScanner.Scan(path, includeSubfolders);
                var groups = FileScanner.GroupFiles(files, subGroupMode);
                var totalDirectoryBytes = groups.Sum(group => group.TotalSizeBytes);
                return groups
                    .Select(group => new FileGroupViewModel(group, thumbnailPixelSize, totalDirectoryBytes))
                    .ToList();
            });

            ClearSelection();
            ArchivePreviewCache.Clear();
            FileGroups.Clear();
            PendingDeletionFiles.Clear();
            PendingBackupFiles.Clear();

            foreach (var group in builtGroups)
                FileGroups.Add(group);

            ApplyGroupSort();

            var allEntries = FileGroups.SelectMany(g => g.AllFiles).ToList();

            foreach (var entry in allEntries)
            {
                if (pendingDeletionPaths.Contains(entry.Entry.FullPath) && File.Exists(entry.Entry.FullPath))
                    ApplyPendingDeletion(entry, addToPendingCollection: true);

                if (pendingBackupPaths.Contains(entry.Entry.FullPath) && File.Exists(entry.Entry.FullPath))
                    ApplyPendingBackup(entry, addToPendingCollection: true);
            }

            CleanupMissingPendingPaths(allEntries);
            CleanupMissingPendingBackupPaths(allEntries);
            UpdatePendingTabHeader();
            UpdatePendingBackupTabHeader();
            UpdateStatusSummary();
        }
        catch (Exception ex)
        {
            StatusText = $"오류: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MarkForDeletion(object? parameter)
    {
        var selected = GetSelectedActiveFiles();
        if (selected.Count > 0)
        {
            MarkFilesForDeletion(selected);
            return;
        }

        if (parameter is FileEntryViewModel single && single.IsActiveInList)
            MarkFilesForDeletion([single]);
    }

    private void MarkForBackup(object? parameter)
    {
        var selected = GetSelectedActiveFiles();
        if (selected.Count > 0)
        {
            MarkFilesForBackup(selected);
            return;
        }

        if (parameter is FileEntryViewModel single && single.IsActiveInList)
            MarkFilesForBackup([single]);
    }

    private void MarkFilesForDeletion(IReadOnlyList<FileEntryViewModel> targets)
    {
        foreach (var file in targets)
            ApplyPendingDeletion(file, addToPendingCollection: true);

        ClearSelection();
        UpdatePendingTabHeader();
        UpdateStatusSummary();
        CommandManager.InvalidateRequerySuggested();
    }

    private void MarkFilesForBackup(IReadOnlyList<FileEntryViewModel> targets)
    {
        foreach (var file in targets)
            ApplyPendingBackup(file, addToPendingCollection: true);

        ClearSelection();
        UpdatePendingBackupTabHeader();
        UpdateStatusSummary();
        NavigateToBackupTabRequested?.Invoke();
        CommandManager.InvalidateRequerySuggested();
    }

    private void ApplyPendingDeletion(FileEntryViewModel file, bool addToPendingCollection)
    {
        if (file.IsPendingDeletion || file.IsPendingBackup)
            return;

        file.IsPendingDeletion = true;
        file.IsSelected = false;
        _pendingDeletionPaths.Add(file.Entry.FullPath);

        if (addToPendingCollection && !PendingDeletionFiles.Contains(file))
            PendingDeletionFiles.Add(file);
    }

    private void ApplyPendingBackup(FileEntryViewModel file, bool addToPendingCollection)
    {
        if (file.IsPendingBackup || file.IsPendingDeletion)
            return;

        file.IsPendingBackup = true;
        file.IsSelected = false;
        _pendingBackupPaths.Add(file.Entry.FullPath);

        if (addToPendingCollection && !PendingBackupFiles.Contains(file))
            PendingBackupFiles.Add(file);
    }

    private void SelectAllInGroup(object? parameter)
    {
        if (parameter is not FileGroupViewModel group)
            return;

        ClearSelection();

        foreach (var file in group.ActiveFiles)
            SetSelected(file, true);

        _selectionAnchor = group.ActiveFiles.LastOrDefault();
        UpdateSelectionText();
        UpdateFilePreview();
    }

    public void ToggleGroupSelection(FileGroupViewModel group)
    {
        var activeFiles = group.ActiveFiles.ToList();
        if (activeFiles.Count == 0)
            return;

        var allSelected = activeFiles.All(file => file.IsSelected);

        ClearSelection();

        if (!allSelected)
        {
            foreach (var file in activeFiles)
                SetSelected(file, true);

            _selectionAnchor = activeFiles.Last();
        }

        UpdateSelectionText();
        UpdateFilePreview();
    }

    public bool? GetGroupSelectionState(FileGroupViewModel group)
    {
        var activeFiles = group.ActiveFiles.ToList();
        if (activeFiles.Count == 0)
            return false;

        var selectedCount = activeFiles.Count(file => file.IsSelected);
        if (selectedCount == 0)
            return false;

        return selectedCount == activeFiles.Count ? true : null;
    }

    private void ClearSelection()
    {
        foreach (var file in _selectedFiles.ToList())
            file.IsSelected = false;

        _selectedFiles.Clear();
        _selectionAnchor = null;
        UpdateSelectionText();
        UpdateFilePreview();
    }

    private void RestoreFromPending(object? parameter)
    {
        var targets = parameter is FileEntryViewModel single
            ? new List<FileEntryViewModel> { single }
            : PendingDeletionFiles.ToList();

        RestorePendingItems(targets);
    }

    private void RestoreFromPendingBackup(object? parameter)
    {
        var targets = parameter is FileEntryViewModel single
            ? new List<FileEntryViewModel> { single }
            : PendingBackupFiles.ToList();

        RestorePendingBackupItems(targets);
    }

    private void OpenFile(object? parameter)
    {
        if (parameter is not FileEntryViewModel entry)
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = entry.Entry.FullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"파일을 열 수 없습니다.\n{ex.Message}", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenContainingFolder()
    {
        if (!Directory.Exists(SelectedPath))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SelectedPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"폴더를 열 수 없습니다.\n{ex.Message}", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool CanInteractWithFile(object? parameter)
        => parameter is FileEntryViewModel entry && File.Exists(entry.Entry.FullPath);

    private bool CanRestoreFromPending(object? parameter)
    {
        if (parameter is FileEntryViewModel entry)
            return entry.IsPendingDeletion;

        return PendingDeletionFiles.Count > 0;
    }

    private bool CanRestoreFromPendingBackup(object? parameter)
    {
        if (parameter is FileEntryViewModel entry)
            return entry.IsPendingBackup;

        return PendingBackupFiles.Count > 0;
    }

    private bool CanMarkForDeletion(object? parameter)
    {
        if (GetSelectedActiveFiles().Count > 0)
            return true;

        return parameter is FileEntryViewModel entry && entry.IsActiveInList;
    }

    private bool CanMarkForBackup(object? parameter)
    {
        if (GetSelectedActiveFiles().Count > 0)
            return true;

        return parameter is FileEntryViewModel entry && entry.IsActiveInList;
    }

    private bool CanExecuteBackup(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(BackupDestinationPath) || !Directory.Exists(BackupDestinationPath))
            return false;

        if (parameter is FileEntryViewModel entry)
            return entry.IsPendingBackup;

        return PendingBackupFiles.Count > 0;
    }

    private IReadOnlyList<FileEntryViewModel> GetSelectedActiveFiles()
        => _selectedFiles.Where(file => file.IsActiveInList).ToList();

    private void SelectRange(
        IReadOnlyList<FileEntryViewModel> activeFiles,
        FileEntryViewModel anchor,
        FileEntryViewModel target)
    {
        var activeList = activeFiles.ToList();
        var start = activeList.IndexOf(anchor);
        var end = activeList.IndexOf(target);

        if (start < 0 || end < 0)
        {
            ToggleSelection(target);
            UpdateSelectionText();
            UpdateFilePreview();
            return;
        }

        if (start > end)
            (start, end) = (end, start);

        ClearSelection();

        for (var i = start; i <= end; i++)
            SetSelected(activeList[i], true);

        UpdateSelectionText();
        UpdateFilePreview();
    }

    private void ToggleSelection(FileEntryViewModel entry)
    {
        SetSelected(entry, !entry.IsSelected);

        if (entry.IsSelected)
            _selectionAnchor = entry;
    }

    private void SetSelected(FileEntryViewModel entry, bool isSelected)
    {
        entry.IsSelected = isSelected;

        if (isSelected)
            _selectedFiles.Add(entry);
        else
            _selectedFiles.Remove(entry);

        entry.ParentGroup?.NotifySelectionStateChanged();
    }

    private void ApplyGroupSort()
    {
        var sorted = _groupSortMode switch
        {
            GroupSortMode.FileCount => FileGroups.OrderByDescending(g => g.FileCount).ToList(),
            GroupSortMode.LatestCreatedDate => FileGroups.OrderByDescending(g => g.LatestCreatedAt).ToList(),
            _ => FileGroups.OrderByDescending(g => g.TotalSizeBytes).ToList()
        };

        FileGroups.Clear();
        foreach (var group in sorted)
            FileGroups.Add(group);
    }

    private void ApplyThumbnailSize()
    {
        var pixelSize = ThumbnailPixelSize;

        foreach (var file in FileGroups.SelectMany(group => group.AllFiles))
            file.ThumbnailPixelSize = pixelSize;

        foreach (var file in PendingDeletionFiles)
            file.ThumbnailPixelSize = pixelSize;

        foreach (var file in PendingBackupFiles)
            file.ThumbnailPixelSize = pixelSize;

        UpdateFilePreview();
    }

    private void RecalculateOccupancyShares()
    {
        var totalDirectoryBytes = FileGroups
            .SelectMany(group => group.AllFiles)
            .Sum(file => file.Entry.SizeBytes);

        if (totalDirectoryBytes <= 0)
            return;

        foreach (var group in FileGroups)
            group.UpdateOccupancy(totalDirectoryBytes);
    }

    private void UpdateFilePreview()
    {
        var selected = _selectedFiles.ToList();
        if (selected.Count == 1)
            FilePreview.LoadFrom(selected[0], PreviewPixelSize);
        else
            FilePreview.Clear();
    }

    private void RemoveFileFromMainList(FileEntryViewModel file)
    {
        if (file.ParentGroup is null)
            return;

        foreach (var subGroup in file.ParentGroup.SubGroups)
            subGroup.Files.Remove(file);
    }

    private void CleanupMissingPendingPaths(IReadOnlyList<FileEntryViewModel> currentEntries)
    {
        var currentPaths = currentEntries
            .Select(entry => entry.Entry.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in _pendingDeletionPaths.ToList())
        {
            if (currentPaths.Contains(path))
                continue;

            if (!File.Exists(path))
                _pendingDeletionPaths.Remove(path);
        }
    }

    private void CleanupMissingPendingBackupPaths(IReadOnlyList<FileEntryViewModel> currentEntries)
    {
        var currentPaths = currentEntries
            .Select(entry => entry.Entry.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in _pendingBackupPaths.ToList())
        {
            if (currentPaths.Contains(path))
                continue;

            if (!File.Exists(path))
                _pendingBackupPaths.Remove(path);
        }
    }

    private void UpdateSelectionText()
    {
        var selected = GetSelectedActiveFiles();
        if (selected.Count == 0)
        {
            SelectionText = string.Empty;
            return;
        }

        var totalSize = selected.Sum(file => file.Entry.SizeBytes);
        SelectionText = $"선택 {selected.Count:N0}개 · {FileSizeFormatter.Format(totalSize)} · Delete 삭제 예정 · Insert 백업 예정";
    }

    private void UpdatePendingTabHeader()
    {
        PendingDeletionTabHeader = $"삭제 예정 ({PendingDeletionFiles.Count:N0})";
    }

    private void UpdatePendingBackupTabHeader()
    {
        PendingBackupTabHeader = $"백업 예정 ({PendingBackupFiles.Count:N0})";
    }

    private void UpdateStatusSummary()
    {
        var activeFiles = FileGroups.SelectMany(group => group.ActiveFiles).ToList();
        var totalActive = activeFiles.Count;
        var totalActiveSize = activeFiles.Sum(file => file.Entry.SizeBytes);
        var pendingDeletionCount = PendingDeletionFiles.Count;
        var pendingDeletionSize = PendingDeletionFiles.Sum(file => file.Entry.SizeBytes);
        var pendingBackupCount = PendingBackupFiles.Count;
        var pendingBackupSize = PendingBackupFiles.Sum(file => file.Entry.SizeBytes);

        if (totalActive == 0 && pendingDeletionCount == 0 && pendingBackupCount == 0)
        {
            StatusText = "표시할 파일이 없습니다.";
            return;
        }

        var parts = new List<string>();

        if (totalActive > 0)
            parts.Add($"{totalActive:N0}개 파일 · {FileSizeFormatter.Format(totalActiveSize)} · {FileGroups.Count}개 그룹");

        if (pendingDeletionCount > 0)
            parts.Add($"삭제 예정 {pendingDeletionCount:N0}개 · {FileSizeFormatter.Format(pendingDeletionSize)}");

        if (pendingBackupCount > 0)
            parts.Add($"백업 예정 {pendingBackupCount:N0}개 · {FileSizeFormatter.Format(pendingBackupSize)}");

        StatusText = string.Join(" | ", parts);
    }
}

public sealed class SubGroupOptionViewModel
{
    public SubGroupOptionViewModel(SubGroupMode mode, string displayName)
    {
        Mode = mode;
        DisplayName = displayName;
    }

    public SubGroupMode Mode { get; }
    public string DisplayName { get; }
}

public sealed class GroupSortOptionViewModel
{
    public GroupSortOptionViewModel(GroupSortMode mode, string displayName)
    {
        Mode = mode;
        DisplayName = displayName;
    }

    public GroupSortMode Mode { get; }
    public string DisplayName { get; }
}

public sealed class ThumbnailSizeOptionViewModel
{
    public ThumbnailSizeOptionViewModel(ThumbnailSize size, string displayName)
    {
        Size = size;
        DisplayName = displayName;
    }

    public ThumbnailSize Size { get; }
    public string DisplayName { get; }
}
