using System.IO;
using System.Windows;
using Microsoft.Win32;
using Models.Entities;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit.Windows;

public partial class AddWorkspaceWindow : Window
{
    public WorkspaceItem ResultWorkspace { get; private set; } = new();
    private readonly WorkspaceItem? sourceWorkspace;


    public AddWorkspaceWindow()
    {
        InitializeComponent();
    }
    
    public AddWorkspaceWindow(WorkspaceItem? workspace) : this()
    {
        sourceWorkspace = workspace;

        if (workspace is null)
            return;

        WindowTitleTextBlock.Text = "Edit workspace";
        NameTextBox.Text = workspace.Name;
        RootPathTextBox.Text = workspace.RootPath;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            AppMessageBox.Show(this, "Предупреждение", "Введите название workspace.");
            return;
        }
        
        if (sourceWorkspace is not null)
        {
            sourceWorkspace.Name = NameTextBox.Text.Trim();
            sourceWorkspace.RootPath = RootPathTextBox.Text.Trim();

            ResultWorkspace = sourceWorkspace;
        }
        else
        {
            ResultWorkspace = new WorkspaceItem
            {
                Name = NameTextBox.Text.Trim(),
                RootPath = RootPathTextBox.Text.Trim(),
                ResumeText = "",
            };
        }

        DialogResult = true;
        Close();
    }

    private void BrowseRootPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выбери папку workspace"
        };

        var currentPath = RootPathTextBox.Text.Trim();
        if (Directory.Exists(currentPath))
            dialog.InitialDirectory = currentPath;

        if (dialog.ShowDialog(this) == true)
            RootPathTextBox.Text = dialog.FolderName;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}