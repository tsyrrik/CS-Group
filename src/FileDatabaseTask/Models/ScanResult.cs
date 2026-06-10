namespace FileDatabaseTask.Models;

public sealed record ScanResult(IReadOnlyList<FileSystemEntry> Entries, IReadOnlyList<string> Warnings);

