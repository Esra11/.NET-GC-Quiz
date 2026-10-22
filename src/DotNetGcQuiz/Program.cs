namespace DotNetGcQuiz;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            if (args.Contains("--smoke-test"))
            {
                UiSmoke.Run(args.Last());
                return;
            }
            if (args.Contains("--report-benchmark"))
            {
                UiSmoke.BenchmarkReport(args.Last());
                return;
            }
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            if (args.Contains("--smoke-test") || args.Contains("--report-benchmark")) throw;
            MessageBox.Show($"The app could not start.\n\n{ex.Message}", ".NET GC Quiz", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
