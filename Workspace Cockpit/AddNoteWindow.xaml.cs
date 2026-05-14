using System.Windows;
using System.Windows.Controls;
using Models;

namespace Workspace_Cockpit;

public partial class AddNoteWindow : Window
{
    public WorkspaceNote CreatedNote { get; private set; } = new();
    public AddNoteWindow()
    {
        InitializeComponent();
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
            MessageBox.Show("Введите текст заметки.");
            return;
        }

        CreatedNote = new WorkspaceNote
        {
            Text = NoteTextBox.Text.Trim()
        };
        
        DialogResult = true;
        Close();
    }
}