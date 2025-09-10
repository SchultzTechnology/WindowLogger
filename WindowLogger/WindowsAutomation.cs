using System.Runtime.InteropServices;
using System.Text;

public class WindowsAutomation
{
    // Struct to store window information
    public class WindowInfo
    {
        public string? Title { get; set; }
        public string? ClassName { get; set; }
        public IntPtr Handle { get; set; }
    }

    // Delegate for EnumWindows
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    // Import EnumWindows from user32.dll
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    // Import GetWindowText from user32.dll
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    // Import IsWindowVisible from user32.dll
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    // Import GetClassName from user32.dll
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    // Gets a list of all open windows
    public static List<WindowInfo> GetOpenWindows()
    {
        var windows = new List<WindowInfo>();

        EnumWindows((hwnd, lParam) =>
        {
            if (IsWindowVisible(hwnd))
            {
                var title = new StringBuilder(256);
                var className = new StringBuilder(256);
                
                GetWindowText(hwnd, title, title.Capacity);
                GetClassName(hwnd, className, className.Capacity);

                if (!string.IsNullOrWhiteSpace(title.ToString()))
                {
                    windows.Add(new WindowInfo
                    {
                        Title = title.ToString(),
                        ClassName = className.ToString(),
                        Handle = hwnd
                    });
                }
            }
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
