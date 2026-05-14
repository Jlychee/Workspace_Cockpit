using System.Windows;
using Models;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit;

public partial class AddActionWindow : Window
{
    public WorkspaceAction? ResultAction { get; private set; } = new();
    
    public AddActionWindow()
    {
        InitializeComponent();
    }


    public AddActionWindow(WorkspaceAction? action) : this()
    {
        NameTextBox.Text = action.Name;
        TargetTextBox.Text = action.Target;
        WorkingDirectoryTextBox.Text = action.WorkingDirectory;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            AppMessageBox.Show(this, "Предупреждение", "Введите название action");
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetTextBox.Text))
        {
            AppMessageBox.Show(this, "Предупреждение","Введите target или command");
            return;
        }
        
        ResultAction = new WorkspaceAction
        {
            Name = NameTextBox.Text.Trim(),
            Target = TargetTextBox.Text.Trim(),
            WorkingDirectory = WorkingDirectoryTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }
    

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}