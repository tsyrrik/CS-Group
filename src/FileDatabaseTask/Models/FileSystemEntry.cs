namespace FileDatabaseTask.Models;

public sealed class FileSystemEntry
{
    public string RootPath { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string? ParentRelativePath { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public long SizeBytes { get; set; }
    public int FilesCount { get; set; }
}

