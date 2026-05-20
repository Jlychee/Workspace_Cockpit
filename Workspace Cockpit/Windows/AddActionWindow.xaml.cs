using System.IO;
using System.Windows;
using Microsoft.Win32;
using Models.Entities;
using Models.Enums;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit.Windows;

public partial class AddActionWindow : Window
{
    private readonly WorkspaceAction? sourceAction;

    public WorkspaceAction ResultAction { get; private set; } = new();

    public AddActionWindow()
    {
        InitializeComponent();

        ActionTypeComboBox.ItemsSource = Enum.GetValues<WorkspaceActionType>();
        ActionTypeComboBox.SelectedItem = WorkspaceActionType.Command;
    }

    public AddActionWindow(WorkspaceAction? action) : this()
    {
        sourceAction = action;

        if (action is null)
            return;

        WindowTitleTextBlock.Text = "Edit action";
        NameTextBox.Text = action.Name;
        TargetTextBox.Text = action.Target;
        WorkingDirectoryTextBox.Text = action.WorkingDirectory;
        ActionTypeComboBox.SelectedItem = action.ActionType;
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
            AppMessageBox.Show(this, "Предупреждение", "Введите target или command");
            return;
        }

        ResultAction = new WorkspaceAction
        {
            Id = sourceAction?.Id ?? 0,
            WorkspaceId = sourceAction?.WorkspaceId ?? 0,
            CreatedAtUtc = sourceAction?.CreatedAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            LastRunAtUtc = sourceAction?.LastRunAtUtc,
            Name = NameTextBox.Text.Trim(),
            ActionType = ActionTypeComboBox.SelectedItem is WorkspaceActionType actionType
                ? actionType
                : WorkspaceActionType.Command,
            Target = TargetTextBox.Text.Trim(),
            WorkingDirectory = WorkingDirectoryTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void BrowseWorkingDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выбери working directory"
        };

        var currentPath = WorkingDirectoryTextBox.Text.Trim();
        if (Directory.Exists(currentPath))
            dialog.InitialDirectory = currentPath;

        if (dialog.ShowDialog(this) == true)
            WorkingDirectoryTextBox.Text = dialog.FolderName;
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
