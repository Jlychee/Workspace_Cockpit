using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Infrastructure.Repositories;
using Models.Entities;
using Models.Enums;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit.Windows;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly WorkspaceRepository workspaceRepository;
    private readonly WorkspaceNoteRepository noteRepository;
    private readonly WorkspaceActionRepository actionRepository;
    private readonly WorkspaceLogRepository logRepository;
    private const string ActionDragFormat = "WorkspaceCockpit.Action";
    private const string NoteDragFormat = "WorkspaceCockpit.Note";
    private Point dragStartPoint;

    public ObservableCollection<WorkspaceItem> Workspaces { get; } = [];
    public ObservableCollection<WorkspaceLog> ActionRuns { get; } = [];

    public WorkspaceItem? SelectedWorkspace
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            field = value;
            SelectedAction = null;
            SelectedNote = null;
            OnPropertyChanged();

            _ = HandleSelectedWorkspaceChangedAsync(value);
        }
    }

    public WorkspaceAction? SelectedAction
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public WorkspaceNote? SelectedNote
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow(WorkspaceRepository repository, WorkspaceRepository workspaceRepository,
        WorkspaceNoteRepository noteRepository, WorkspaceActionRepository actionRepository,
        WorkspaceLogRepository logRepository)
    {
        this.workspaceRepository = workspaceRepository;
        this.noteRepository = noteRepository;
        this.actionRepository = actionRepository;
        this.logRepository = logRepository;
        InitializeComponent();
        DataContext = this;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadWorkspacesAsync();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Database error", ex.Message);
        }
    }

    private async Task LoadWorkspacesAsync()
    {
        var workspaces = await workspaceRepository.LoadWorkspacesAsync();

        Workspaces.Clear();
        foreach (var workspace in workspaces)
            Workspaces.Add(workspace);

        SelectedWorkspace = Workspaces.FirstOrDefault();
    }

    private async Task HandleSelectedWorkspaceChangedAsync(WorkspaceItem? workspace)
    {
        ActionRuns.Clear();

        if (workspace is null)
            return;

        try
        {
            var now = DateTime.UtcNow;
            workspace.LastOpenedAtUtc = now;
            workspace.NotifyDisplayChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));

            if (workspace.Id > 0)
            {
                await workspaceRepository.UpdateWorkspaceLastOpenedAsync(workspace.Id, now);
                await ReloadLogsAsync(workspace.Id);
            }
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Workspace error", ex.Message);
        }
    }

    private async Task ReloadLogsAsync(int workspaceId)
    {
        var runs = await logRepository.LoadLogsAsync(workspaceId);

        ActionRuns.Clear();
        foreach (var run in runs)
            ActionRuns.Add(run);
    }

    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveResume_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        try
        {
            await workspaceRepository.UpdateWorkspaceAsync(SelectedWorkspace);
            SelectedWorkspace.NotifyDisplayChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Save error", ex.Message);
        }
    }

    private async void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedNote is null)
            return;

        try
        {
            var note = SelectedNote;
            await noteRepository.DeleteNoteAsync(note);
            SelectedWorkspace.Notes.Remove(note);
            TouchSelectedWorkspace();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Delete note error", ex.Message);
        }
    }

    private async void AddNote_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        var window = new AddNoteWindow { Owner = this };

        if (window.ShowDialog() != true)
            return;

        try
        {
            await noteRepository.AddNoteAsync(SelectedWorkspace.Id, window.ResultNote);
            SelectedWorkspace.Notes.Add(window.ResultNote);
            TouchSelectedWorkspace(window.ResultNote.UpdatedAtUtc);
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Add note error", ex.Message);
        }
    }

    private async void RunAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkspaceAction action })
            await RunWorkspaceActionAsync(action);
    }

    private async void RunAllActions_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        foreach (var action in SelectedWorkspace.Actions.ToList())
            await RunWorkspaceActionAsync(action);
    }

    private async Task RunWorkspaceActionAsync(WorkspaceAction action)
    {
        if (SelectedWorkspace is null)
            return;

        try
        {
            var run = await ExecuteActionAsync(action);
            await logRepository.AddLogAsync(action, run);

            action.NotifyDisplayChanged();
            SelectedWorkspace.NotifyDisplayChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
            ActionRuns.Insert(0, run);
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Run action error", ex.Message);
        }
    }

    private async Task<WorkspaceLog> ExecuteActionAsync(WorkspaceAction action)
    {
        var run = new WorkspaceLog
        {
            WorkspaceId = action.WorkspaceId,
            WorkspaceActionId = action.Id,
            ActionNameSnapshot = action.Name,
            TargetSnapshot = action.Target,
            StartedAtUtc = DateTime.UtcNow,
            Status = "Started"
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            switch (action.ActionType)
            {
                case WorkspaceActionType.Command:
                    await RunCommandAsync(action, run);
                    break;

                case WorkspaceActionType.File:
                    OpenFileAction(action, run);
                    break;

                case WorkspaceActionType.Folder:
                    OpenFolderAction(action, run);
                    break;

                case WorkspaceActionType.Url:
                    OpenUrlAction(action, run);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported action type: {action.ActionType}");
            }

            return run;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.ErrorMessage = ex.Message;
            return run;
        }
        finally
        {
            stopwatch.Stop();
            run.FinishedAtUtc = DateTime.UtcNow;
            run.DurationMs = stopwatch.ElapsedMilliseconds;
        }
    }

    private async Task RunCommandAsync(WorkspaceAction action, WorkspaceLog run)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {action.Target}",
            WorkingDirectory = ResolveWorkingDirectory(action),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            
            StandardOutputEncoding = Encoding.GetEncoding(866),
            StandardErrorEncoding = Encoding.GetEncoding(866)
        };

        await RunProcessAsync(process, run);
    }


    private static async Task RunProcessAsync(Process process, WorkspaceLog run)
    {
        if (!process.Start())
            throw new InvalidOperationException("Process was not started.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        run.ExitCode = process.ExitCode;
        run.Status = process.ExitCode == 0 ? "Completed" : "Failed";
        run.OutputPreview = ToPreview(output);
        run.ErrorMessage = process.ExitCode == 0
            ? ""
            : ToErrorMessage(process.ExitCode, error);
    }

    private void OpenFileAction(WorkspaceAction action, WorkspaceLog run)
    {
        var filePath = ResolveActionPath(action);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File was not found.", filePath);

        OpenShellTarget(filePath);
        run.Status = "Completed";
        run.OutputPreview = $"Opened file: {filePath}";
    }

    private void OpenFolderAction(WorkspaceAction action, WorkspaceLog run)
    {
        var folderPath = ResolveActionPath(action);
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder was not found: {folderPath}");

        OpenShellTarget(folderPath);
        run.Status = "Completed";
        run.OutputPreview = $"Opened folder: {folderPath}";
    }

    private static void OpenUrlAction(WorkspaceAction action, WorkspaceLog run)
    {
        var uri = ResolveHttpUri(action.Target);

        OpenShellTarget(uri.AbsoluteUri);
        run.Status = "Completed";
        run.OutputPreview = $"Opened url: {uri.AbsoluteUri}";
    }

    private string ResolveWorkingDirectory(WorkspaceAction action, bool allowFallback = true)
    {
        if (!string.IsNullOrWhiteSpace(action.WorkingDirectory) && Directory.Exists(action.WorkingDirectory))
            return action.WorkingDirectory;

        if (SelectedWorkspace is not null &&
            !string.IsNullOrWhiteSpace(SelectedWorkspace.RootPath) &&
            Directory.Exists(SelectedWorkspace.RootPath))
        {
            return SelectedWorkspace.RootPath;
        }

        return allowFallback
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : "";
    }

    private string ResolveActionPath(WorkspaceAction action)
    {
        var target = Environment.ExpandEnvironmentVariables(action.Target.Trim());
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Action target is empty.");

        if (Path.IsPathFullyQualified(target))
            return Path.GetFullPath(target);

        return Path.GetFullPath(Path.Combine(ResolveWorkingDirectory(action), target));
    }

    private static Uri ResolveHttpUri(string target)
    {
        var value = target.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Url is empty.");

        if (!value.Contains("://", StringComparison.Ordinal))
            value = $"https://{value}";

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Url is invalid: {target}");
        }

        return uri;
    }

    private static void OpenShellTarget(string target)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static string ToPreview(string value)
    {
        var preview = value.Trim();
        return preview.Length <= 4000
            ? preview
            : preview[..4000];
    }

    private static string ToErrorMessage(int exitCode, string error)
    {
        var preview = ToPreview(error);
        return string.IsNullOrWhiteSpace(preview)
            ? $"Process exited with code {exitCode}."
            : preview;
    }

    private async void EditAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedAction is null)
        {
            AppMessageBox.Show(this, "Предупреждение", "Выбери action для изменения");
            return;
        }

        var window = new AddActionWindow(SelectedAction)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        try
        {
            var updatedAction = window.ResultAction;

            SelectedAction.Name = updatedAction.Name;
            SelectedAction.Target = updatedAction.Target;
            SelectedAction.WorkingDirectory = updatedAction.WorkingDirectory;
            SelectedAction.ActionType = updatedAction.ActionType;
            SelectedAction.UpdatedAtUtc = updatedAction.UpdatedAtUtc;

            await actionRepository.UpdateActionAsync(SelectedAction);

            SelectedAction.NotifyDisplayChanged();
            TouchSelectedWorkspace(SelectedAction.UpdatedAtUtc);
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Edit action error", ex.Message);
        }
    }

    private async void DeleteAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedAction is null)
            return;

        try
        {
            var action = SelectedAction;
            await actionRepository.DeleteActionAsync(action);
            SelectedWorkspace.Actions.Remove(action);
            await ReloadLogsAsync(SelectedWorkspace.Id);
            TouchSelectedWorkspace();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Delete action error", ex.Message);
        }
    }

    private async void AddAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        var window = new AddActionWindow
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        try
        {
            await actionRepository.AddActionAsync(SelectedWorkspace.Id, window.ResultAction);
            SelectedWorkspace.Actions.Add(window.ResultAction);
            SelectedAction = window.ResultAction;
            TouchSelectedWorkspace(window.ResultAction.UpdatedAtUtc);
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Add action error", ex.Message);
        }
    }

    private async void DeleteWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        var workspace = SelectedWorkspace;
        
        try
        {
            await workspaceRepository.DeleteWorkspaceAsync(workspace);

            Workspaces.Remove(workspace);

            SelectedWorkspace = Workspaces.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Delete workspace error", ex.Message);
        }
    }

    private async void NewWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddWorkspaceWindow()
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        try
        {
            await workspaceRepository.AddWorkspaceAsync(window.ResultWorkspace);
            Workspaces.Add(window.ResultWorkspace);
            SelectedWorkspace = window.ResultWorkspace;
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Add workspace error", ex.Message);
        }
    }

    private void TouchSelectedWorkspace(DateTime? updatedAtUtc = null)
    {
        if (SelectedWorkspace is null)
            return;

        SelectedWorkspace.UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
        SelectedWorkspace.NotifyDisplayChanged();
        OnPropertyChanged(nameof(SelectedWorkspace));
    }

    private void ReorderList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(this);
    }

    private void ActionList_MouseMove(object sender, MouseEventArgs e)
    {
        StartListDrag<WorkspaceAction>(sender, e, ActionDragFormat);
    }

    private void NoteList_MouseMove(object sender, MouseEventArgs e)
    {
        StartListDrag<WorkspaceNote>(sender, e, NoteDragFormat);
    }

    private void ActionList_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragEffects(e, ActionDragFormat);
    }

    private void NoteList_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragEffects(e, NoteDragFormat);
    }

    private async void ActionList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(ActionDragFormat) is not WorkspaceAction action)
            return;

        await ReorderItemsAsync(
            sender,
            e,
            SelectedWorkspace?.Actions,
            action,
            static (item, index) => item.SortOrder = index,
            actionRepository.UpdateActionOrderAsync,
            "Reorder actions error");
    }

    private async void NoteList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(NoteDragFormat) is not WorkspaceNote note)
            return;

        await ReorderItemsAsync(
            sender,
            e,
            SelectedWorkspace?.Notes,
            note,
            static (item, index) => item.SortOrder = index,
            noteRepository.UpdateNoteOrderAsync,
            "Reorder notes error");
    }

    private void StartListDrag<T>(object sender, MouseEventArgs e, string dataFormat)
        where T : class
    {
        if (sender is not ListBox || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (e.OriginalSource is not DependencyObject source)
            return;

        if (FindVisualParent<ButtonBase>(source) is not null)
            return;

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var listBoxItem = FindVisualParent<ListBoxItem>(source);
        if (listBoxItem?.DataContext is not T item)
            return;

        DragDrop.DoDragDrop(listBoxItem, new DataObject(dataFormat, item), DragDropEffects.Move);
    }

    private async Task ReorderItemsAsync<T>(
        object sender,
        DragEventArgs e,
        ObservableCollection<T>? collection,
        T draggedItem,
        Action<T, int> setSortOrder,
        Func<int, IReadOnlyList<T>, Task> saveOrderAsync,
        string errorTitle)
        where T : class
    {
        e.Handled = true;

        if (SelectedWorkspace is null || sender is not ListBox || collection is null)
            return;

        var sourceIndex = collection.IndexOf(draggedItem);
        if (sourceIndex < 0)
            return;

        var dropIndex = GetDropIndex(e, collection);
        if (dropIndex > sourceIndex)
            dropIndex--;

        dropIndex = Math.Clamp(dropIndex, 0, collection.Count - 1);
        if (dropIndex == sourceIndex)
            return;

        var workspaceId = SelectedWorkspace.Id;
        var originalItems = collection.ToList();

        collection.Move(sourceIndex, dropIndex);
        NormalizeSortOrder(collection, setSortOrder);

        try
        {
            await saveOrderAsync(workspaceId, collection.ToList());
            TouchSelectedWorkspace();
        }
        catch (Exception ex)
        {
            RestoreCollection(collection, originalItems);
            NormalizeSortOrder(collection, setSortOrder);
            AppMessageBox.Show(this, errorTitle, ex.Message);
        }
    }

    private static int GetDropIndex<T>(DragEventArgs e, ObservableCollection<T> collection)
        where T : class
    {
        if (e.OriginalSource is not DependencyObject source)
            return collection.Count;

        var targetContainer = FindVisualParent<ListBoxItem>(source);
        if (targetContainer?.DataContext is not T targetItem)
            return collection.Count;

        var targetIndex = collection.IndexOf(targetItem);
        if (targetIndex < 0)
            return collection.Count;

        var targetPosition = e.GetPosition(targetContainer);
        return targetPosition.Y > targetContainer.ActualHeight / 2
            ? targetIndex + 1
            : targetIndex;
    }

    private static void UpdateDragEffects(DragEventArgs e, string dataFormat)
    {
        e.Effects = e.Data.GetDataPresent(dataFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private static void NormalizeSortOrder<T>(IReadOnlyList<T> items, Action<T, int> setSortOrder)
    {
        for (var index = 0; index < items.Count; index++)
            setSortOrder(items[index], index);
    }

    private static void RestoreCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }

    private static T? FindVisualParent<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
                return match;

            source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return null;
    }

    private async void EditNote_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedNote is null)
        {
            AppMessageBox.Show(this, "Предупреждение", "Выбери note для изменения");
            return;
        }

        var window = new AddNoteWindow(SelectedNote)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        try
        {
            var updatedNote = window.ResultNote;

            SelectedNote.Text = updatedNote.Text;
            SelectedNote.Type = updatedNote.Type;
            SelectedNote.UpdatedAtUtc = updatedNote.UpdatedAtUtc;

            await noteRepository.UpdateNoteAsync(SelectedNote);

            SelectedNote.NotifyDisplayChanged();
            TouchSelectedWorkspace(SelectedNote.UpdatedAtUtc);
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Edit note error", ex.Message);
        }
    }

    // TODO: костыль...

    private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var eventArg = new MouseWheelEventArgs(
            e.MouseDevice,
            e.Timestamp,
            e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };

        MainScrollViewer.RaiseEvent(eventArg);
    }

    private async void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        await logRepository.ClearLogsAsync(SelectedWorkspace.Id);
        ActionRuns.Clear();
    }

    private async void RemoveSelectedLog_Click(object sender, RoutedEventArgs e)
    {
        if (LogList.SelectedItem is WorkspaceLog selected)
        {
            await logRepository.DeleteLogAsync(selected);
            ActionRuns.Remove(selected);
        }
    }

    private async void EditWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;
        
        var window = new AddWorkspaceWindow(SelectedWorkspace)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        try
        {
            await workspaceRepository.UpdateWorkspaceAsync(window.ResultWorkspace);

            SelectedWorkspace.NotifyDisplayChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Edit workspace error", ex.Message);
        }
    }

    private void CopyError_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string errorText })
            return;

        if (string.IsNullOrWhiteSpace(errorText))
            return;

        Clipboard.SetText(errorText);
    }
}
