namespace FileDatabaseTask.Models;

public enum CompareStatus
{
    Unchanged,
    New,
    Deleted,
    SizeChanged,
    DirectoryAggregateChanged
}

public static class CompareStatusLabels
{
    public const string Unchanged = "Без изменений";
    public const string New = "Новый";
    public const string Deleted = "Удален";
    public const string SizeChanged = "Изменился размер";
    public const string DirectoryAggregateChanged = "Изменилась папка";

    public static string FromStatus(CompareStatus status)
    {
        return status switch
        {
            CompareStatus.New => New,
            CompareStatus.Deleted => Deleted,
            CompareStatus.SizeChanged => SizeChanged,
            CompareStatus.DirectoryAggregateChanged => DirectoryAggregateChanged,
            _ => Unchanged
        };
    }
}
