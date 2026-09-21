// Every converted P/Invoke, called for real. LibraryImport generates ExactSpelling = true, so a
// wrong entry point is an EntryPointNotFoundException at the call and nothing at compile time.
using System.Drawing;

using Core.Enums;
using Core.Models.Business;
using Core.Ports;

using Platform.Windows.Native;
using Platform.Windows.SystemActions;
using Platform.Windows.Windowing;

int failed = 0;

void Check(string name, Action run)
{
    try
    {
        run();
        Console.WriteLine($"  ok    {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"  FAIL  {name}: {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine("ScreenMetrics");
Check("SetProcessDpiAwarenessContext", () => ScreenMetrics.EnablePerMonitorDpiAwareness());
Check("GetDpiForMonitor (+ the two unconverted enum calls)", () =>
{
    IReadOnlyList<MonitorInfo> monitors = ScreenMetrics.GetAllMonitors();
    Console.WriteLine($"        {monitors.Count} monitor(s)");
});

Console.WriteLine("WindowService");
WindowService windows = new WindowService();

Check("GetForegroundWindow", () =>
{
    string? title = windows.GetForegroundWindowTitle();
    Console.WriteLine($"        foreground: \"{title}\"");
});

Core.Models.Business.WindowHandle sample = default;
Check("IsWindowVisible + GetWindowThreadProcessId", () =>
{
    IReadOnlyList<SystemWindow> all = windows.GetApplicationWindows();
    Console.WriteLine($"        {all.Count} window(s)");
    // A handle for the bounds calls below: the foreground window, which always exists.
    sample = windows.FindWindow(new WindowQuery { TitleMatchMode = TitleMatchModeEnum.CONTAINS, TitlePattern = string.Empty });
});

Check("GetWindowRect", () =>
{
    Rectangle bounds = windows.GetWindowBounds(sample, useClientArea: false);
    Console.WriteLine($"        window rect: {bounds}");
});

Check("GetClientRect + ClientToScreen", () =>
{
    Rectangle bounds = windows.GetWindowBounds(sample, useClientArea: true);
    Console.WriteLine($"        client rect: {bounds}");
});

// SetWindowPos / SetForegroundWindow / ShowWindow / IsIconic / PostMessage all move or close a
// real window, so they are resolved rather than invoked: a missing export throws on the first
// call, and Marshal.Prelink forces that without the side effect.
Console.WriteLine("Entry points resolved without invoking (they move or close real windows)");
foreach (string name in new[] { "PostMessage", "SetForegroundWindow", "IsIconic", "ShowWindow", "SetWindowPos" })
{
    Check(name, () =>
    {
        System.Reflection.MethodInfo method = typeof(WindowService).GetMethod(
            name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        System.Runtime.InteropServices.Marshal.Prelink(method);
    });
}

Console.WriteLine("SystemActionService (MONITOR_ON is the harmless one)");
Check("SendMessageTimeoutW", () => new SystemActionService().Run(SystemActionTypeEnum.MONITOR_ON));

Console.WriteLine("NativeCursor, through IInputService");
IInputService input = new Platform.Windows.Input.InputService();
Check("GetCursorPos", () =>
{
    Point at = input.CursorPosition();
    Console.WriteLine($"        cursor: {at}");
});

Console.WriteLine();
Console.WriteLine(failed == 0 ? "ALL ENTRY POINTS RESOLVED" : $"{failed} FAILED");
return failed;
