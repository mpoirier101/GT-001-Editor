using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace GT001.Editor.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        UnhandledException += OnUnhandledException;

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

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            WriteStartupLog("Launch requested.");
            _window = new MainWindow();
            WriteStartupLog("MainWindow created.");
            _window.Activate();
            WriteStartupLog("MainWindow activated.");
        }
        catch (Exception ex)
        {
            WriteStartupLog($"Launch failed: {ex}");
            throw;
        }
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        WriteStartupLog($"Unhandled exception: {args.Exception}");
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
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
