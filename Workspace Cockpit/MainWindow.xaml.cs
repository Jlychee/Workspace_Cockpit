using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Infrastructure.Repositories;
using Models;
using Models.Entities;
using Models.Enums;
using Workspace_Cockpit.Helpers;
using Workspace_Cockpit.Windows;

namespace Workspace_Cockpit;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly WorkspaceRepository _repository;
    private WorkspaceItem? selectedWorkspace;
    private WorkspaceAction? selectedAction;
    private WorkspaceNote? selectedNote;

    public ObservableCollection<WorkspaceItem> Workspaces { get; } = [];
    public ObservableCollection<WorkspaceActionRun> ActionRuns { get; } = [];

    public WorkspaceItem? SelectedWorkspace
    {
        get => selectedWorkspace;
        set
        {
            if (ReferenceEquals(selectedWorkspace, value))
                return;

            selectedWorkspace = value;
            SelectedAction = null;
            SelectedNote = null;
            OnPropertyChanged();

            _ = HandleSelectedWorkspaceChangedAsync(value);
        }
    }

    public WorkspaceAction? SelectedAction
    {
        get => selectedAction;
        set
        {
            selectedAction = value;
            OnPropertyChanged();
        }
    }

    public WorkspaceNote? SelectedNote
    {
        get => selectedNote;
        set
        {
            selectedNote = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow(WorkspaceRepository repository)
    {
        _repository = repository;
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
        var workspaces = await _repository.LoadWorkspacesAsync();

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
                await _repository.UpdateWorkspaceLastOpenedAsync(workspace.Id, now);
                await ReloadActionRunsAsync(workspace.Id);
            }
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(this, "Workspace error", ex.Message);
        }
    }

    private async Task ReloadActionRunsAsync(int workspaceId)
    {
        var runs = await _repository.LoadActionRunsAsync(workspaceId);

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
            await _repository.UpdateWorkspaceAsync(SelectedWorkspace);
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
            await _repository.DeleteNoteAsync(note);
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
            await _repository.AddNoteAsync(SelectedWorkspace.Id, window.CreatedNote);
            SelectedWorkspace.Notes.Add(window.CreatedNote);
            TouchSelectedWorkspace(window.CreatedNote.UpdatedAtUtc);
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

    private async void RunSelectedAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAction is null)
        {
            AppMessageBox.Show(this, "Предупреждение", "Выбери action для запуска");
            return;
        }

        await RunWorkspaceActionAsync(SelectedAction);
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
            await _repository.AddActionRunAsync(action, run);

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

    private async Task<WorkspaceActionRun> ExecuteActionAsync(WorkspaceAction action)
    {
        var run = new WorkspaceActionRun
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
            if (action.ActionType is WorkspaceActionType.File or WorkspaceActionType.Folder or WorkspaceActionType.Url)
            {
                StartShellAction(action);
                run.Status = "Completed";
                run.OutputPreview = "Started via shell.";
                return run;
            }

            await RunCommandAsync(action, run);
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

    private async Task RunCommandAsync(WorkspaceAction action, WorkspaceActionRun run)
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
        run.ErrorMessage = process.ExitCode == 0 ? "" : ToPreview(error);
    }

    private void StartShellAction(WorkspaceAction action)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = action.Target,
            UseShellExecute = true
        };

        var workingDirectory = ResolveWorkingDirectory(action, allowFallback: false);
        if (!string.IsNullOrWhiteSpace(workingDirectory))
            startInfo.WorkingDirectory = workingDirectory;

        if (Process.Start(startInfo) is null)
            throw new InvalidOperationException("Shell action was not started.");
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

    private static string ToPreview(string value)
    {
        var preview = value.Trim();
        return preview.Length <= 4000
            ? preview
            : preview[..4000];
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

            await _repository.UpdateActionAsync(SelectedAction);

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
            await _repository.DeleteActionAsync(action);
            SelectedWorkspace.Actions.Remove(action);
            await ReloadActionRunsAsync(SelectedWorkspace.Id);
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
            await _repository.AddActionAsync(SelectedWorkspace.Id, window.ResultAction);
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
            await _repository.AddWorkspaceAsync(window.ResultWorkspace);
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
}
