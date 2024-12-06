using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// A collection of methods to access the Windows32 API using interop, unmanaged function calls (PInvoke). 
/// <para>Most, if not all, the functions were inherited from daunting legacy code.
/// Detailed documentation for a specific function or it's inner working can likely be found on <see href="https://www.pinvoke.net/">PInvoke.net</see>
/// </para>
/// <para>
/// Documentation for methods related to UI (showing a window, sending a message) can be found by searching through this <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/">index on MSdocs</see>
/// </para>
/// </summary>
public class Windows32API
{
    #region Win32API

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    /// <summary>
    /// Primarily used to find top-level windows
    /// For windows that are children of top-level windows, try <seealso cref="FindWindowEx(IntPtr, IntPtr, string, string)"/>
    /// </summary>
    /// <param name="lpClassName"></param>
    /// <param name="lpWindowName"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    /// <summary>
    ///  retrieves the integrity level of a specified process by checking the integrity of a token handle associated with a process' handle.
    /// </summary>
    /// <param name="processHandle"></param>
    /// <returns></returns>
    /// <exception cref="System.ComponentModel.Win32Exception"></exception>
    public static int GetProcessHandleIntegrityLevel(IntPtr processHandle)
    {
        if (!OpenProcessToken(processHandle, 0x02000000 | 0x00000008, out IntPtr tokenHandle))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            if (!GetTokenInformation(tokenHandle, ProcessToken.TokenIntegrityLevel, IntPtr.Zero, 0, out uint returnLength))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    throw new System.ComponentModel.Win32Exception(error);
                }
            }

            IntPtr tokenInformation = Marshal.AllocHGlobal((int)returnLength);
            try
            {
                if (!GetTokenInformation(tokenHandle, ProcessToken.TokenIntegrityLevel, tokenInformation, returnLength, out _))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                TOKEN_MANDATORY_LABEL tokenMandatoryLabel = (TOKEN_MANDATORY_LABEL)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_MANDATORY_LABEL));
                SID_AND_ATTRIBUTES sidAndAttributes = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(tokenMandatoryLabel.Label.Sid, typeof(SID_AND_ATTRIBUTES));

                // Integrity level: 0 = Untrusted, 1 = Low, 2 = Medium, 3 = High, 4 = System
                return (int)sidAndAttributes.Attributes >> 16;
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                CloseHandle(tokenHandle);
            }
        }
    }

    /// <summary>
    /// Retrieves a specified type of information about an access token for a running process.
    /// Primarily used to determine whether a running process was started with elevated UAC priveleges.
    /// <see href="https://www.pinvoke.net/default.aspx/advapi32/gettokeninformation.html">More details</see>
    /// </summary>
    /// <param name="TokenHandle"></param>
    /// <param name="processToken"></param>
    /// <param name="TokenInformation"></param>
    /// <param name="TokenInformationLength"></param>
    /// <param name="ReturnLength"></param>
    /// <returns></returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool GetTokenInformation(
        IntPtr TokenHandle,
        ProcessToken processToken,
        IntPtr TokenInformation,
        uint TokenInformationLength,
        out uint ReturnLength);

    /// <summary>
    /// Find window, used for finding direct child windows of a parent window. Does NOT search through descendant windows.
    /// <para><see href=">">Read the docs</see> for a detailed explanation and parameter overview</para>
    /// </summary>
    /// <param name="hwndParent">Window handle of the main window</param>
    /// <param name="hwndChildAfter">Window handler of the control</param>
    /// <param name="lpszClass"></param>
    /// <param name="lpszWindow"></param>
    /// <returns></returns>
    [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    /// <summary>
    /// Retrieves a handle to a window whose class name and window name match the specified strings.
    /// The function searches child windows, beginning with the one following the specified child window.
    /// This function does not perform a case-sensitive search.
    /// <para><see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-findwindowexw">Read the docs</see></para>
    /// </summary>
    /// <param name="hwndParent"></param>
    /// <param name="hwndChildAfter"></param>
    /// <param name="lpszClass"></param>
    /// <param name="lpszWindow"></param>
    /// <returns></returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindowExW(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

    [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, int wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern bool SendNotifyMessage(HandleRef hWnd, uint Msg, IntPtr wParam, string lParam);

    /// <summary>
    /// Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);


    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetFocus(HandleRef hWnd);


    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetFocus(IntPtr hWnd);


    public static IntPtr FindWindowByIndex(IntPtr hWndParent, int index, string controlclassName)
    {
        if (index == 0)
            return hWndParent;
        else
        {
            int ct = 0;
            IntPtr result = IntPtr.Zero;
            do
            {
                //result = FindWindowEx(hWndParent, result, "Button", null);
                result = FindWindowEx(hWndParent, result, controlclassName, null);
                if (result != IntPtr.Zero)
                    ++ct;
            }
            while (ct < index && result != IntPtr.Zero);
            return result;
        }
    }

    public static Process[] GetWindowsHandle(string process)
    {
        try
        {
            Process[] runningProcesses = Process.GetProcesses();
            var currentSessionID = Process.GetCurrentProcess().SessionId;

            //Process[] WinCRMProcess;
            //Filter the process for Cornerstone process for current user only.
            Process[] WinCRMProcess = (from c in runningProcesses where c.SessionId == currentSessionID && string.Equals(c.ProcessName, process, StringComparison.CurrentCultureIgnoreCase) select c).ToArray();
            return WinCRMProcess;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Array.Empty<Process>();
        }
    }
    /// <summary>
    /// Determines whether a specified process is running with elevated UAC privileges which would prevent certain interactions like SendKeys or UI Automation from working as expected.
    /// An integrity level of 2 and above is considered an elevated UAC privilege
    /// The integrity level is represented as follows:
    /// <list type="bullet">
    /// <item>0: Untrusted</item>
    /// <item>1: Low</item>
    /// <item>2: Medium</item>
    /// <item>3: High</item>
    /// <item>4: System</item>
    /// </list>
    /// </summary>
    /// <param name="process"></param>
    /// <returns>bool that determines whether UAC elevated privelege (>= 2). false if specified process or it's handle are null</returns>
    public static bool IsProcessHandleRunningInElevatedUAC(IntPtr processHandle)
    {
        const int mediumPrivilege = 2;
        if (processHandle == IntPtr.Zero)
        {
            return false;
        }
        try
        {
            int integrityLevel = GetProcessHandleIntegrityLevel(processHandle);
            // Check if the process is running with elevated UAC privileges
            return integrityLevel >= mediumPrivilege; // 2 or higher indicates elevated privileges
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return false;
        }
    }


    public const uint WM_SETTEXT = 0x000C;
    public const int BN_CLICKED = 245;
    public const int WM_KEYDOWN = 0x100;
    public const int VK_TAB = 0x09;//0x0D
    public const int VK_Enter = 0x0D;
    public const int WM_GETTEXT = 0x000D;
    public const int WM_GETTEXTLENGTH = 0x000E;

    /// <summary>
    /// Token information for a running process. Primarily used to determine whether a running process was started with elevated UAC priveleges
    /// <see href="https://www.pinvoke.net/default.aspx/advapi32/gettokeninformation.html">More details</see>
    /// </summary>
    public enum ProcessToken
    {
        TokenUser = 1,
        TokenIntegrityLevel = 25,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_MANDATORY_LABEL
    {
        public SID_AND_ATTRIBUTES Label;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }
    public enum ShowWindowState : int
    {
        SW_MINIMIZE = 6,
        SW_RESTORE = 9,
        SW_SHOW = 5,
        SW_MAXIMIZE = 3,
        SW_SHOWNORMAL = 1
    }
    #endregion
}