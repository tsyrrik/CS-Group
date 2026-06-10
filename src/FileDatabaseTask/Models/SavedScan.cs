namespace FileDatabaseTask.Models;

public sealed record SavedScan(ScanHeader Header, IReadOnlyList<FileSystemEntry> Items);

