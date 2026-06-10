namespace FileDatabaseTask.Models;

public sealed class FileGridRow
{
    public string Status { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string ParentRelativePath { get; init; } = string.Empty;
    public long SavedSizeBytes { get; init; }
    public long CurrentSizeBytes { get; init; }
    public int FilesCount { get; init; }

    public static FileGridRow FromEntry(
        FileSystemEntry entry,
        CompareStatus status = CompareStatus.Unchanged,
        long? savedSizeBytes = null,
        long? currentSizeBytes = null)
    {
        return new FileGridRow
        {
            Status = CompareStatusLabels.FromStatus(status),
            Type = entry.IsDirectory ? "Папка" : "Файл",
            Name = entry.Name,
            RelativePath = entry.RelativePath,
            ParentRelativePath = entry.ParentRelativePath ?? string.Empty,
            SavedSizeBytes = savedSizeBytes ?? entry.SizeBytes,
            CurrentSizeBytes = currentSizeBytes ?? entry.SizeBytes,
            FilesCount = entry.FilesCount
        };
    }
}

