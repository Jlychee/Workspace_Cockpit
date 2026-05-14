using System.Windows;

namespace Workspace_Cockpit.Helpers;

public static class AppMessageBox
{
    public static void Show(Window owner, string title, string message)
    {
        var window = new AppMessageBoxWindow(title, message)
        {
            Owner = owner
        };
        
        window.ShowDialog();
    }
}