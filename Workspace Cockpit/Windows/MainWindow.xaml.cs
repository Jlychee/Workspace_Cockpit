using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
            CreateNoWindow = true
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
}