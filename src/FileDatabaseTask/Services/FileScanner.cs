using FileDatabaseTask.Models;

namespace FileDatabaseTask.Services;

public sealed class FileScanner
{
    public ScanResult Scan(
        string rootPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Не указан путь для сканирования.", nameof(rootPath));
        }

        var normalizedRootPath = Path.GetFullPath(rootPath);
        var rootDirectory = new DirectoryInfo(normalizedRootPath);

        if (!rootDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Папка не найдена: {normalizedRootPath}");
        }

        var context = new ScanContext(rootDirectory.FullName, entries: [], warnings: [], progress, cancellationToken);
        ScanDirectory(rootDirectory, null, context);

        var entries = context.Entries
            .OrderBy(entry => entry.RelativePath == "." ? string.Empty : entry.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ScanResult(entries, context.Warnings);
    }

    private static FileSystemEntry ScanDirectory(
        DirectoryInfo directory,
        string? parentRelativePath,
        ScanContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var relativePath = GetRelativePath(context.RootPath, directory.FullName);
        var directoryEntry = new FileSystemEntry
        {
            RootPath = context.RootPath,
            FullPath = directory.FullName,
            RelativePath = relativePath,
            ParentRelativePath = parentRelativePath,
            Name = GetDirectoryName(directory),
            IsDirectory = true
        };

        context.AddEntry(directoryEntry);

        long totalSizeBytes = 0;
        var filesCount = 0;

        foreach (var file in GetFiles(directory, context.Warnings))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (IsReparsePoint(file, context.Warnings))
            {
                context.Warnings.Add($"Пропущена ссылка/переход: {file.FullName}");
                continue;
            }

            if (!TryGetFileSize(file, context.Warnings, out var fileSize))
            {
                continue;
            }

            totalSizeBytes += fileSize;
            filesCount++;

            context.AddEntry(new FileSystemEntry
            {
                RootPath = context.RootPath,
                FullPath = file.FullName,
                RelativePath = GetRelativePath(context.RootPath, file.FullName),
                ParentRelativePath = relativePath,
                Name = file.Name,
                IsDirectory = false,
                SizeBytes = fileSize,
                FilesCount = 0
            });
        }

        foreach (var childDirectory in GetDirectories(directory, context.Warnings))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (IsReparsePoint(childDirectory, context.Warnings))
            {
                context.Warnings.Add($"Пропущена ссылка/переход: {childDirectory.FullName}");
                continue;
            }

            var childEntry = ScanDirectory(childDirectory, relativePath, context);
            totalSizeBytes += childEntry.SizeBytes;
            filesCount += childEntry.FilesCount;
        }

        directoryEntry.SizeBytes = totalSizeBytes;
        directoryEntry.FilesCount = filesCount;

        return directoryEntry;
    }

    private static string GetRelativePath(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        return NormalizeRelativePath(relativePath);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string GetDirectoryName(DirectoryInfo directory)
    {
        return string.IsNullOrWhiteSpace(directory.Name)
            ? directory.FullName
            : directory.Name;
    }

    private static IReadOnlyList<FileInfo> GetFiles(DirectoryInfo directory, ICollection<string> warnings)
    {
        try
        {
            return directory.GetFiles();
        }
        catch (Exception exception) when (IsFileSystemAccessException(exception))
        {
            warnings.Add($"Не удалось прочитать файлы в папке: {directory.FullName}. {exception.Message}");
            return [];
        }
    }

    private static IReadOnlyList<DirectoryInfo> GetDirectories(DirectoryInfo directory, ICollection<string> warnings)
    {
        try
        {
            return directory.GetDirectories();
        }
        catch (Exception exception) when (IsFileSystemAccessException(exception))
        {
            warnings.Add($"Не удалось прочитать вложенные папки: {directory.FullName}. {exception.Message}");
            return [];
        }
    }

    private static bool TryGetFileSize(FileInfo file, ICollection<string> warnings, out long fileSize)
    {
        try
        {
            fileSize = file.Length;
            return true;
        }
        catch (Exception exception) when (IsFileSystemAccessException(exception))
        {
            fileSize = 0;
            warnings.Add($"Не удалось получить размер файла: {file.FullName}. {exception.Message}");
            return false;
        }
    }

    private static bool IsReparsePoint(FileSystemInfo fileSystemInfo, ICollection<string> warnings)
    {
        try
        {
            return fileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception exception) when (IsFileSystemAccessException(exception))
        {
            warnings.Add($"Не удалось прочитать атрибуты: {fileSystemInfo.FullName}. {exception.Message}");
            return false;
        }
    }

    private static bool IsFileSystemAccessException(Exception exception)
    {
        return exception is UnauthorizedAccessException
            or DirectoryNotFoundException
            or FileNotFoundException
            or PathTooLongException
            or IOException;
    }

    private sealed class ScanContext(
        string rootPath,
        List<FileSystemEntry> entries,
        List<string> warnings,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        public string RootPath { get; } = rootPath;

        public List<FileSystemEntry> Entries { get; } = entries;

        public List<string> Warnings { get; } = warnings;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public void AddEntry(FileSystemEntry entry)
        {
            Entries.Add(entry);
            progress?.Report(Entries.Count);
        }
    }
}
