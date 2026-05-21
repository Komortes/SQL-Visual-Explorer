namespace SQLVisualExplorer.Infrastructure.Database;

public static class LocalDatabase
{
    public static string CreateDefaultConnectionString()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var databaseDirectory = Path.Combine(appDataPath, "SQLVisualExplorer");
        var databasePath = Path.Combine(databaseDirectory, "sqlvisualexplorer.db");

        Directory.CreateDirectory(databaseDirectory);

        return $"Data Source={databasePath}";
    }
}
