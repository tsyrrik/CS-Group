using FileDatabaseTask.Models;

namespace FileDatabaseTask.Services;

public sealed class ScanComparer
{
    public IReadOnlyList<FileGridRow> Compare(
        IReadOnlyCollection<FileSystemEntry> savedEntries,
        IReadOnlyCollection<FileSystemEntry> currentEntries)
    {
        var savedByPath = savedEntries.ToDictionary(GetComparisonKey, StringComparer.OrdinalIgnoreCase);
        var currentByPath = currentEntries.ToDictionary(GetComparisonKey, StringComparer.OrdinalIgnoreCase);
        var rows = new List<FileGridRow>();

        foreach (var currentEntry in currentEntries)
        {
            if (!savedByPath.TryGetValue(GetComparisonKey(currentEntry), out var savedEntry))
            {
                rows.Add(FileGridRow.FromEntry(
                    currentEntry,
                    CompareStatus.New,
                    savedSizeBytes: 0,
                    currentSizeBytes: currentEntry.SizeBytes));

                continue;
            }

            var status = ResolveStatus(savedEntry, currentEntry);

            rows.Add(FileGridRow.FromEntry(
                currentEntry,
                status,
                savedEntry.SizeBytes,
                currentEntry.SizeBytes));
        }

        foreach (var savedEntry in savedEntries)
        {
            if (currentByPath.ContainsKey(GetComparisonKey(savedEntry)))
            {
                continue;
            }

            rows.Add(FileGridRow.FromEntry(
                savedEntry,
                CompareStatus.Deleted,
                savedEntry.SizeBytes,
                currentSizeBytes: 0));
        }

        return rows
            .OrderBy(row => row.RelativePath == "." ? string.Empty : row.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Type, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetComparisonKey(FileSystemEntry entry)
    {
        return $"{entry.RelativePath}|{entry.IsDirectory}";
    }

    private static CompareStatus ResolveStatus(FileSystemEntry savedEntry, FileSystemEntry currentEntry)
    {
        if (savedEntry.SizeBytes == currentEntry.SizeBytes && savedEntry.FilesCount == currentEntry.FilesCount)
        {
            return CompareStatus.Unchanged;
        }

        return currentEntry.IsDirectory
            ? CompareStatus.DirectoryAggregateChanged
            : CompareStatus.SizeChanged;
    }
}
