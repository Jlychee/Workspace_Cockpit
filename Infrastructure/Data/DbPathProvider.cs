namespace Infrastructure.Data;

public static class DbPathProvider
{
    public static string GetDatabaseDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Workspace Cockpit");
    }

    public static string GetDatabasePath()
    {
        return Path.Combine(GetDatabaseDirectory(), "workspace-cockpit.db");
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }
}
