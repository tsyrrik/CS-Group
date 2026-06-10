namespace FileDatabaseTask.Data;

public static class Schema
{
    private const string SchemaFileName = "schema.sql";

    public static string Load()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "db", SchemaFileName);
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"SQL-схема не найдена: {schemaPath}", schemaPath);
        }

        return File.ReadAllText(schemaPath);
    }
}
