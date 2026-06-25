using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FolderTidy.ViewModels;

namespace FolderTidy;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        viewModel.NavigateToBackupTabRequested += () => MainTabControl.SelectedIndex = 2;
        DataContext = viewModel;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ViewModel.RefreshCommand.Execute(null);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox)
            return;

        if (e.Key == Key.Insert && MainTabControl.SelectedIndex == 0)
        {
            ViewModel.MarkSelectedForBackup();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Delete)
            return;

        if (MainTabControl.SelectedIndex == 0)
        {
            ViewModel.MarkSelectedForDeletion();
            e.Handled = true;
            return;
        }

        if (MainTabControl.SelectedIndex == 1)
        {
            var selected = GetPendingDeletionSelectedItems();
            if (selected.Count > 0)
            {
                ViewModel.PermanentlyDeletePending(selected);
                e.Handled = true;
            }
        }
    }

    private void FileCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not FileEntryViewModel entry)
            return;

        if (MainTabControl.SelectedIndex != 0)
            return;

        if (e.ClickCount > 1)
            return;

        if (IsClickFromInteractiveControl(e.OriginalSource))
            return;

        ViewModel.HandleFileSelection(entry, Keyboard.Modifiers);
        e.Handled = true;
    }

    private void FileName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        if (sender is not FrameworkElement element || element.DataContext is not FileEntryViewModel entry)
            return;

        if (MainTabControl.SelectedIndex != 0)
            return;

        ViewModel.OpenFileCommand.Execute(entry);
        e.Handled = true;
    }

    private void FileSelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not FileEntryViewModel entry)
            return;

        if (MainTabControl.SelectedIndex != 0)
            return;

        ViewModel.HandleFileSelection(entry, Keyboard.Modifiers | ModifierKeys.Control);
        e.Handled = true;
    }

    private void PermanentlyDeleteSinglePendingButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FileEntryViewModel entry })
            return;

        ViewModel.PermanentlyDeletePending([entry]);
    }

    private void ExecuteBackupSinglePendingButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FileEntryViewModel entry })
            return;

        ViewModel.ExecuteBackupPending([entry]);
    }

    private void CategoryExpander_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander { DataContext: FileGroupViewModel group })
            group.LoadThumbnails();
    }

    private void GroupSelectAllCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void GroupSelectAllCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.Tag is not FileGroupViewModel group)
            return;

        ViewModel.ToggleGroupSelection(group);
        checkBox.IsChecked = ViewModel.GetGroupSelectionState(group);
        e.Handled = true;
    }

    private void RestorePendingButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetPendingDeletionSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("복원할 파일을 선택해 주세요.", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ViewModel.RestorePendingItems(selected);
        PendingDeletionListBox.UnselectAll();
    }

    private void PermanentlyDeletePendingButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetPendingDeletionSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("삭제할 파일을 선택해 주세요.", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ViewModel.PermanentlyDeletePending(selected);
        PendingDeletionListBox.UnselectAll();
    }

    private void RestorePendingBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetPendingBackupSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("복원할 파일을 선택해 주세요.", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ViewModel.RestorePendingBackupItems(selected);
        PendingBackupListBox.UnselectAll();
    }

    private void ExecuteBackupPendingButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetPendingBackupSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("백업할 파일을 선택해 주세요.", "FolderTidy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ViewModel.ExecuteBackupPending(selected);
        PendingBackupListBox.UnselectAll();
    }

    private List<FileEntryViewModel> GetPendingDeletionSelectedItems()
        => PendingDeletionListBox.SelectedItems.Cast<FileEntryViewModel>().ToList();

    private List<FileEntryViewModel> GetPendingBackupSelectedItems()
        => PendingBackupListBox.SelectedItems.Cast<FileEntryViewModel>().ToList();

    private static bool IsClickFromInteractiveControl(object? originalSource)
    {
        var current = originalSource as DependencyObject;

        while (current is not null)
        {
            if (current is CheckBox or Button)
                return true;

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
