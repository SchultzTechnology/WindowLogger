using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

public class WindowsAutomation
{
    // Struct to store window information
    public class WindowInfo
    {
        public string? Title { get; set; }
        public string? ClassName { get; set; }
        public IntPtr Handle { get; set; }
    }

    public class ChildControlInfo
    {
        public string? ClassName { get; set; }
        public string? Text { get; set; }
        public IntPtr Handle { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsInput { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ControlId { get; set; }
        public string? AutomationName { get; set; }
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetDlgCtrlID(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_GETTEXT = 0x000D;
    private const uint WM_GETTEXTLENGTH = 0x000E;

    private static string GetControlText(IntPtr hwnd)
    {
        int len = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        
        SendMessage(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
        return sb.ToString();
    }

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

    private static readonly HashSet<string> InputClassNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Edit", "RichEdit", "RichEdit20A", "RichEdit20W", "RICHEDIT50W",
        "ComboBox", "ComboBoxEx32",
        "msctls_trackbar32",
        "SysDateTimePick32",
        "SysListView32",
        "SysTreeView32",
        // Delphi (VCL) input controls
        "TEdit", "TMemo", "TRichEdit", "TMaskEdit",
        "TComboBox", "TListBox", "TCheckListBox",
        "TDateTimePicker", "TSpinEdit",
        "TStringGrid", "TDrawGrid",
    };

    public static List<ChildControlInfo> GetChildControls(IntPtr windowHandle)
    {
        var controls = new List<ChildControlInfo>();

        EnumChildWindows(windowHandle, (hwnd, lParam) =>
        {
            var className = new StringBuilder(256);
            GetClassName(hwnd, className, className.Capacity);
            var cls = className.ToString();

            var text = GetControlText(hwnd);

            GetWindowRect(hwnd, out RECT rect);

            var isInput = InputClassNames.Contains(cls);
            string? automationName = null;
            if (isInput)
            {
                try
                {
                    var element = AutomationElement.FromHandle(hwnd);
                    automationName = element.Current.Name;
                }
                catch { }
            }

            controls.Add(new ChildControlInfo
            {
                ClassName = cls,
                Text = text,
                Handle = hwnd,
                IsVisible = IsWindowVisible(hwnd),
                IsEnabled = IsWindowEnabled(hwnd),
                IsInput = isInput,
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top,
                ControlId = GetDlgCtrlID(hwnd),
                AutomationName = automationName,
            });

            return true;
        }, IntPtr.Zero);

        return controls;
    }
}
