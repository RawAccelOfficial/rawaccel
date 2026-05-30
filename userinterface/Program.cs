using Avalonia;
using System;
using System.IO;
using System.Threading.Tasks;

namespace userinterface;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // This is for crash logging. It Installs global exception sinks BEFORE
        // Avalonia starts so a crash during startup or on a worker thread should still get
        // written to logs/crash.log before the process exits.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void WriteCrashLog(string source, Exception? ex)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Never rethrow from a terminal exception handler.
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}