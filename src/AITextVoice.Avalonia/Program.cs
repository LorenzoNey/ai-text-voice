using Avalonia;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AITextVoice;

class Program
{
    private const string MutexName = "AITextVoice_SingleInstance_Mutex";
    private static Mutex? _mutex;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        // Check for existing instance using a named mutex
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            ShowAlreadyRunningMessage();
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    private static void ShowAlreadyRunningMessage()
    {
        const string message = "AITextVoice is already running.\n\nCheck the system tray for the existing instance.";
        const string title = "AITextVoice";

        if (OperatingSystem.IsWindows())
        {
            // Use Windows MessageBox via P/Invoke
            try
            {
                _ = MessageBox(IntPtr.Zero, message, title, 0x00000040); // MB_ICONINFORMATION
            }
            catch
            {
                Console.WriteLine(message);
            }
        }
        else
        {
            // For other platforms, write to console
            Console.WriteLine(message);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // Windows MessageBox P/Invoke - only called on Windows at runtime
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
