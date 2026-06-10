using FileDatabaseTask.Models;
using Npgsql;
using NpgsqlTypes;

namespace FileDatabaseTask.Data;

public sealed class ScanRepository
{
    public void EnsureCreated()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(Schema.Load(), connection);

        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ScanHeader> GetScans()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(
            """
            SELECT id, root_path, scanned_at
            FROM scans
            ORDER BY scanned_at DESC, id DESC;
            """,
            connection);
        using var reader = command.ExecuteReader();

        var scans = new List<ScanHeader>();
        while (reader.Read())
        {
            scans.Add(new ScanHeader(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDateTime(2).ToLocalTime()));
        }

        return scans;
    }

    public int SaveScan(string rootPath, IReadOnlyCollection<FileSystemEntry> entries)
    {
        if (entries.Count == 0)
        {
            throw new InvalidOperationException("Нет данных для сохранения.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var scanId = CreateScan(connection, transaction, rootPath);
        SaveItems(connection, scanId, entries);

        transaction.Commit();

        return scanId;
    }

    public SavedScan LoadScan(int scanId)
    {
        using var connection = OpenConnection();
        var header = LoadScanHeader(connection, scanId);
        var items = LoadScanItems(connection, scanId, header.RootPath);

        return new SavedScan(header, items);
    }

    private static NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(DatabaseOptions.ConnectionString);
        connection.Open();

        return connection;
    }

    private static int CreateScan(NpgsqlConnection connection, NpgsqlTransaction transaction, string rootPath)
    {
        using var command = new NpgsqlCommand(
            """
            INSERT INTO scans (root_path, scanned_at)
            VALUES (@root_path, @scanned_at)
            RETURNING id;
            """,
            connection,
            transaction);

        command.Parameters.Add("root_path", NpgsqlDbType.Text).Value = rootPath;
        command.Parameters.Add("scanned_at", NpgsqlDbType.TimestampTz).Value = DateTime.UtcNow;

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void SaveItems(
        NpgsqlConnection connection,
        int scanId,
        IEnumerable<FileSystemEntry> entries)
    {
        using var writer = connection.BeginBinaryImport(
            """
            COPY scan_items (
                scan_id,
                relative_path,
                full_path,
                parent_relative_path,
                name,
                is_directory,
                size_bytes,
                files_count
            )
            FROM STDIN (FORMAT BINARY);
            """);

        foreach (var entry in entries)
        {
            writer.StartRow();
            writer.Write(scanId, NpgsqlDbType.Integer);
            writer.Write(entry.RelativePath, NpgsqlDbType.Text);
            writer.Write(entry.FullPath, NpgsqlDbType.Text);

            if (entry.ParentRelativePath is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.Write(entry.ParentRelativePath, NpgsqlDbType.Text);
            }

            writer.Write(entry.Name, NpgsqlDbType.Text);
            writer.Write(entry.IsDirectory, NpgsqlDbType.Boolean);
            writer.Write(entry.SizeBytes, NpgsqlDbType.Bigint);
            writer.Write(entry.FilesCount, NpgsqlDbType.Integer);
        }

        writer.Complete();
    }

    private static ScanHeader LoadScanHeader(NpgsqlConnection connection, int scanId)
    {
        using var command = new NpgsqlCommand(
            """
            SELECT id, root_path, scanned_at
            FROM scans
            WHERE id = @id;
            """,
            connection);

        command.Parameters.Add("id", NpgsqlDbType.Integer).Value = scanId;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException($"Сканирование с ID {scanId} не найдено.");
        }

        return new ScanHeader(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetDateTime(2).ToLocalTime());
    }

    private static IReadOnlyList<FileSystemEntry> LoadScanItems(NpgsqlConnection connection, int scanId, string rootPath)
    {
        using var command = new NpgsqlCommand(
            """
            SELECT
                relative_path,
                full_path,
                parent_relative_path,
                name,
                is_directory,
                size_bytes,
                files_count
            FROM scan_items
            WHERE scan_id = @scan_id
            ORDER BY relative_path ASC, is_directory DESC;
            """,
            connection);

        command.Parameters.Add("scan_id", NpgsqlDbType.Integer).Value = scanId;

        using var reader = command.ExecuteReader();
        var items = new List<FileSystemEntry>();

        while (reader.Read())
        {
            items.Add(new FileSystemEntry
            {
                RootPath = rootPath,
                RelativePath = reader.GetString(0),
                FullPath = reader.GetString(1),
                ParentRelativePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                Name = reader.GetString(3),
                IsDirectory = reader.GetBoolean(4),
                SizeBytes = reader.GetInt64(5),
                FilesCount = reader.GetInt32(6)
            });
        }

        return items;
    }
}
