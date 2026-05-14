using System.Windows;
using Models;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit;

public partial class AddWorkspaceWindow : Window
{
    public WorkspaceItem ResultWorkspace { get; private set; } = new();

    public AddWorkspaceWindow()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            AppMessageBox.Show(this, "Предупреждение" ,"Введите название workspace.");
            return;
        }

        ResultWorkspace = new WorkspaceItem
        {
            Name = NameTextBox.Text.Trim(),
            RootPath = RootPathTextBox.Text.Trim(),
            Meta = "0 actions · 0 notes",
            ResumeText = "",
        };

        DialogResult = true;
        Close();
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