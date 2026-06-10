namespace FileDatabaseTask.Data;

public static class DatabaseOptions
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=54325;Database=file_scans;Username=file_scans;Password=file_scans";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("FILE_DATABASE_CONNECTION_STRING")
        ?? DefaultConnectionString;
}

