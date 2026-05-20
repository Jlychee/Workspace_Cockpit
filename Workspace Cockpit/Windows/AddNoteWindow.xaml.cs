using System.Windows;
using Models.Entities;
using Workspace_Cockpit.Helpers;

namespace Workspace_Cockpit.Windows;

public partial class AddNoteWindow : Window
{
    public WorkspaceNote ResultNote { get; private set; } = new();

    public AddNoteWindow()
    {
        InitializeComponent();

        NoteTypeComboBox.ItemsSource = WorkspaceNoteTypes.All;
        NoteTypeComboBox.SelectedItem = WorkspaceNoteTypes.General;
    }

    public AddNoteWindow(WorkspaceNote? note) : this()
    {
        if (note is null)
            return;

        WindowTitleTextBlock.Text = "Edit note";
        NoteTypeComboBox.SelectedItem = note.Type;
        NoteTextBox.Text = note.Text;
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

        ResultNote = new WorkspaceNote
        {
            Type = NoteTypeComboBox.SelectedItem as string ?? WorkspaceNoteTypes.General,
            Text = NoteTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
