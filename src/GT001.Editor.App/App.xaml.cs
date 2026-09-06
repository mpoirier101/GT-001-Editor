using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GT001.Editor.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        try
        {
            WriteStartupLog("App initializing.");
            InitializeComponent();
            WriteStartupLog("App initialized.");
        }
        catch (Exception ex)
        {
            WriteStartupLog($"App initialization failed: {ex}");
            throw;
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        WriteStartupLog($"Unhandled exception: {args.Exception}");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            WriteStartupLog("Launch requested.");
            var window = new MainWindow();
            WriteStartupLog("MainWindow created.");
            MainWindow = window;
            window.Show();
            WriteStartupLog("MainWindow activated.");
        }
        catch (Exception ex)
        {
            WriteStartupLog($"Launch failed: {ex}");
            throw;
        }
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GT-001",
                "logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, "startup.log");
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O}\t{message}{Environment.NewLine}");
        }
        catch
        {
            // Startup diagnostics must not create a new startup failure.
        }
    }
}
