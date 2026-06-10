using FileDatabaseTask.Data;

namespace FileDatabaseTask;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(new ScanRepository()));
    }
}

