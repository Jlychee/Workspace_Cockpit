using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        UpdateActionTypeUi();
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
        UpdateActionTypeUi();
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

        var actionType = GetSelectedActionType();

        ResultAction = new WorkspaceAction
        {
            Id = sourceAction?.Id ?? 0,
            WorkspaceId = sourceAction?.WorkspaceId ?? 0,
            CreatedAtUtc = sourceAction?.CreatedAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            LastRunAtUtc = sourceAction?.LastRunAtUtc,
            Name = NameTextBox.Text.Trim(),
            ActionType = actionType,
            Target = TargetTextBox.Text.Trim(),
            WorkingDirectory = actionType == WorkspaceActionType.Url
                ? ""
                : WorkingDirectoryTextBox.Text.Trim()
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

    private void ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionTypeUi();
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        switch (GetSelectedActionType())
        {
            case WorkspaceActionType.File:
                BrowseTargetFile();
                break;

            case WorkspaceActionType.Folder:
                BrowseTargetFolder();
                break;
        }
    }

    private void BrowseTargetFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose file"
        };

        var currentTarget = TargetTextBox.Text.Trim();
        if (File.Exists(currentTarget))
        {
            dialog.FileName = currentTarget;
        }
        else
        {
            var initialDirectory = ResolveInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDirectory))
                dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog(this) == true)
            TargetTextBox.Text = dialog.FileName;
    }

    private void BrowseTargetFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose folder"
        };

        var currentTarget = TargetTextBox.Text.Trim();
        if (Directory.Exists(currentTarget))
            dialog.InitialDirectory = currentTarget;
        else
        {
            var initialDirectory = ResolveInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDirectory))
                dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog(this) == true)
            TargetTextBox.Text = dialog.FolderName;
    }

    private void UpdateActionTypeUi()
    {
        var actionType = GetSelectedActionType();

        WorkingDirectoryPanel.Visibility = actionType == WorkspaceActionType.Url
            ? Visibility.Collapsed
            : Visibility.Visible;

        BrowseTargetButton.Visibility = actionType is WorkspaceActionType.File or WorkspaceActionType.Folder
            ? Visibility.Visible
            : Visibility.Collapsed;

        TargetLabelTextBlock.Text = actionType switch
        {
            WorkspaceActionType.Command => "Command",
            WorkspaceActionType.File => "File path",
            WorkspaceActionType.Folder => "Folder path",
            WorkspaceActionType.Url => "Url",
            _ => "Target"
        };
    }

    private WorkspaceActionType GetSelectedActionType()
    {
        return ActionTypeComboBox.SelectedItem is WorkspaceActionType actionType
            ? actionType
            : WorkspaceActionType.Command;
    }

    private string ResolveInitialDirectory()
    {
        var workingDirectory = WorkingDirectoryTextBox.Text.Trim();
        if (Directory.Exists(workingDirectory))
            return workingDirectory;

        var currentTarget = TargetTextBox.Text.Trim();
        if (File.Exists(currentTarget))
            return Path.GetDirectoryName(currentTarget) ?? "";

        return Directory.Exists(currentTarget)
            ? currentTarget
            : "";
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
