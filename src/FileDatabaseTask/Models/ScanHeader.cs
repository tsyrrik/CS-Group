namespace FileDatabaseTask.Models;

public sealed record ScanHeader(int Id, string RootPath, DateTime ScannedAt)
{
    public string DisplayName => $"{Id}: {ScannedAt:yyyy-MM-dd HH:mm:ss} - {RootPath}";
}

