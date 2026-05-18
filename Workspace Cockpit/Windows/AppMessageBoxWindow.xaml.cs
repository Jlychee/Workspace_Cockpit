using System.Windows;

namespace Workspace_Cockpit.Windows;

public partial class AppMessageBoxWindow : Window
{
    public AppMessageBoxWindow()
    {
        InitializeComponent();
    }

    public AppMessageBoxWindow(string title, string message) : this()
    {
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        Title = title;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}