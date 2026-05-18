using System.Windows;
using Models.Entities;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit.Windows;

public partial class AddNoteWindow : Window
{
    public WorkspaceNote CreatedNote { get; private set; } = new();

    public AddNoteWindow()
    {
        InitializeComponent();

        NoteTypeComboBox.ItemsSource = WorkspaceNoteTypes.All;
        NoteTypeComboBox.SelectedItem = WorkspaceNoteTypes.General;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTextBox.Text))
        {
            AppMessageBox.Show(this, "Предупреждение", "Введите текст заметки.");
            return;
        }

        CreatedNote = new WorkspaceNote
        {
            Type = NoteTypeComboBox.SelectedItem as string ?? WorkspaceNoteTypes.General,
            Text = NoteTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }
}
