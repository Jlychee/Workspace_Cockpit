using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Models;

namespace Workspace_Cockpit;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<WorkspaceItem> Workspaces { get; } = [];
    public WorkspaceItem? selectedWorkspace;
    public WorkspaceAction? selectedAction;
    public WorkspaceNote? selectedNote;

    public WorkspaceItem? SelectedWorkspace
    {
        get => selectedWorkspace;
        set
        {
            selectedWorkspace = value;
            OnPropertyChanged();
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

    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        CreateTestData();

        SelectedWorkspace = Workspaces.FirstOrDefault();
        DataContext = this;
    }

    private void CreateTestData()
    {
        var workspace = new WorkspaceItem
        {
            Name = "Action Inbox",
            RootPath = @"C:\Projects\ActionInbox",
            Meta = "Вчера · 4 actions · 3 notes",
            ResumeText =
                "Остановился на OpenTelemetry. Следующий шаг — добавить trace для GenerateActionsJob и проверить span в Aspire Dashboard.",
        };

        workspace.Actions.Add(new WorkspaceAction
        {
            Name = "Open IDE",
            Type = "Open File",
            Target = @"C:\Projects\ActionInbox\ActionInbox.sln",
            WorkingDirectory = @"C:\Projects\ActionInbox"
        });

        workspace.Actions.Add(new WorkspaceAction
        {
            Name = "Open terminal",
            Type = "Run Command",
            Target = @"wt.exe -d C:\Projects\ActionInbox",
            WorkingDirectory = @"C:\Projects\ActionInbox"
        });

        workspace.Actions.Add(new WorkspaceAction
        {
            Name = "Run docker",
            Type = "Run Command",
            Target = "docker compose up -d",
            WorkingDirectory = @"C:\Projects\ActionInbox"
        });

        workspace.Notes.Add(new WorkspaceNote
        {
            Type = "General",
            Text = "Проверить ActivitySource name — возможно, из-за него trace не отображается."
        });

        workspace.Notes.Add(new WorkspaceNote
        {
            Type = "Runbook",
            Text = "Если Dashboard не открывается — сначала запустить AppHost."
        });

        Workspaces.Add(workspace);

        Workspaces.Add(new WorkspaceItem
        {
            Name = "Изучение Redis",
            RootPath = @"C:\Projects\RedisSandbox",
            Meta = "3 дня назад · 0 actions · 0 notes",
            ResumeText = "Разобраться с persistence и pub/sub.",
        });
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

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedNote is null)
            return;

        SelectedWorkspace.Notes.Remove(SelectedNote);
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null)
            return;

        var window = new AddNoteWindow { Owner = this };

        var result = window.ShowDialog();
        
        if (result != true)
            return;

        SelectedWorkspace.Notes.Add(window.CreatedNote);
    }

    private void RunAction_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void RunSelectedAction_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void EditAction_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void DeleteAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedAction is null)
            return;

        SelectedWorkspace.Actions.Remove(SelectedAction);
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspace is null || SelectedAction is null)
            return;

        SelectedWorkspace.Actions.Add(SelectedAction);
    }
}