using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Automation;
using WindowsInput.Native;

/// <summary>
/// A wrapper around the System.Windows.Automation namespaced UI Automation internal library that allows programmatic access and control of external GUIs
/// </summary>
public class WindowsAutomation
{
    /// <summary>
    /// Get all running processes for specified process name.
    /// An array of processes is returned instead of a single process so that the consumer can handle error cases when no processes or multiple processes are running.
    /// </summary>
    /// <returns>Process[]</returns>
    public Process[] GetRunningProcessesByName(string processName)
    {
        //FInd current session Id based on current process i.e. Find The Chart Process.
        var currentSessionID = Process.GetCurrentProcess().SessionId;
        return Process.GetProcessesByName(processName).Where(p => p.MainWindowHandle != IntPtr.Zero && p.SessionId == currentSessionID).ToArray();
    }
    /// <summary>
    /// Retrieves the AutomationElement-based window element from an IntPtr handle
    /// </summary>
    /// <param name="handle"></param>
    /// <returns>AutomationElement or null</returns>
    public AutomationElement? GetWindowElementFromHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            return AutomationElement.FromHandle(handle);
        }
        catch (InvalidOperationException)
        {
            // Element may not be available if handle is in an invalid-state
            return null;
        }
    }
    /// <summary>
    /// Find the Menu Bar from a Window element.
    /// Note that the SystemMenu is also contained in a Menu Bar, it can be determined using the Element Name -- it contains: "System" instead of "Application".
    /// <see href="https://stackoverflow.com/questions/56269704/ui-automation-control-desktop-application-and-click-on-menu-strip">Source</see>
    /// </summary>
    /// <param name="element"></param>
    /// <returns>AutomationElement or null</returns>
    public AutomationElement? FindMenuBarInWindowElement(AutomationElement element)
    {
        var menuBarCondition = new AndCondition(
            new PropertyCondition(AutomationElement.NameProperty, "Application"),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuBar));
        return element.FindFirst(TreeScope.Children, menuBarCondition);
    }
    /// <summary>
    /// Finds the top-level menu item in a Menu Bar.
    /// <example>e.g. Find the "File" or "View" menu item in an application's Menu Bar</example>
    /// </summary>
    /// <param name="menuBar"></param>
    /// <param name="menuName"></param>
    /// <returns>Found menu item AutomationElement or null</returns>
    public AutomationElement? FindMenuItemByName(AutomationElement menuBar, string menuName)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
            new PropertyCondition(AutomationElement.NameProperty, menuName)
        );
        if (menuBar.Current.ControlType != ControlType.MenuBar)
        {
            return null;
        }
        return menuBar.FindFirst(TreeScope.Children, condition);
    }
    /// <summary>
    /// Tries to invoke/expand sub-menu items for a specific menu-item if possible.
    /// <example>expand the sub-menu items for top-level "File" or "View" menu-item in a Menu Bar</example>
    /// </summary>
    /// <param name="menu"></param>
    public void ExpandSubMenu(AutomationElement menu)
    {
        try
        {
            if (menu.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object pattern))
            {
                ((ExpandCollapsePattern)pattern).Expand();
            }
        }
        catch (InvalidOperationException)
        {
            // If menu element hasn't finished loading, trying to expand it can throw an exception!
            // /In addition, some menus are just toolbar32 window panes which cannot be expanded/collapsed the same way so UI Automation tries a best estimate.
            // src: https://stackoverflow.com/questions/9324619/uiautomation-strange-expandcollapse-behavior
            // If expand/collapse fails try an invoke pattern as a fallback.
            InvokeElement(menu);
        }
    }
    public IEnumerable<AutomationElement> FindSubMenuItemCollection(AutomationElement menuItem)
    {
        if (menuItem == null || menuItem.Current.ControlType != ControlType.MenuItem)
        {
            return new List<AutomationElement>();
        }
        ExpandSubMenu(menuItem);
        var subMenuItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem);
        return menuItem.FindAll(TreeScope.Descendants, subMenuItemCondition).OfType<AutomationElement>();
    }
    /// <summary>
    /// Invokes (i.e. press or click) an AutomationElement
    /// </summary>
    /// <param name="element"></param>
    /// <returns>bool indicating whether invoke action was succesful</returns>
    public bool InvokeElement(AutomationElement element)
    {
        if (element == null)
        {
            return false;
        }

        try
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
            {
                ((InvokePattern)pattern).Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Finds and invokes a sub-menu item from a top-level menu item, if found.
    /// <see href="https://stackoverflow.com/questions/56269704/ui-automation-control-desktop-application-and-click-on-menu-strip">Source</see>
    /// <para>Note: When the exact match flag is true, performance may be substantially better since it performs a linear string search rather than a higher-order sub-string match!</para>
    /// </summary>
    /// <param name="menuItem"></param>
    /// <param name="menuName"></param>
    /// <param name="useExactMatch"></param>
    /// <returns>Whether the sub-menu item was successfully invoked</returns>
    public bool InvokeSubMenuItemByName(AutomationElement menuItem, string menuName, bool useExactMatch = true)
    {
        if (menuItem == null)
        {
            return false;
        }
        var subMenus = FindSubMenuItemCollection(menuItem);
        AutomationElement? subMenuItem;
        if (useExactMatch)
        {
            subMenuItem = subMenus.FirstOrDefault(item => item.Current.Name.Equals(menuName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            subMenuItem = subMenus.FirstOrDefault(item => item.Current.Name.Contains(menuName));
        }

        if (subMenuItem == null)
        {
            return false;
        }
        return InvokeElement(subMenuItem);
    }


    private WindowPattern? GetWindowPattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(WindowPattern.Pattern, out object pattern))
            {
                var windowPattern = (WindowPattern)pattern;
                // Make sure the element is usable and ready.
                // WARNING: if window is stalled for some reason and doesn't idle (v unlikely, but possible), this will block and wait for the specified millisecond duration.
                if (false == windowPattern.WaitForInputIdle(10000))
                {
                    // Object not responding in a timely manner
                    return null;
                }
                return windowPattern;
            }
            else
            {
                return null;
            }
        }
        catch (InvalidOperationException)
        {
            // object doesn't support the WindowPattern control pattern
            return null;
        }
    }
    /// <summary>
    /// Uses the WindowInteractionState to determine how to set the window to foreground while maintaining prior window state.
    /// </summary>
    /// <param name="element"></param>
    /// <returns>bool</returns>
    public bool TrySettingElementWindowToForeground(AutomationElement element)
    {
        var windowPattern = GetWindowPattern(element);
        if (windowPattern == null)
        {
            return false;
        }
        var current = windowPattern.Current;

        if (current.WindowInteractionState == WindowInteractionState.ReadyForUserInteraction
            && !current.IsModal)
        {
            try
            {
                switch (current.WindowVisualState)
                {

                    case WindowVisualState.Minimized:
                        if (current.CanMaximize)
                        {
                            windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
                            return true;
                        }
                        return false;
                    case WindowVisualState.Normal:
                    case WindowVisualState.Maximized:
                    default:
                        element.SetFocus();
                        return true;
                }
            }
            catch (Win32Exception)
            {
                // Can occur if Access is denied due to insufficient UAC priveleges
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
        return false;
    }
    /// <summary>
    /// Sets the element to the foreground while maintaining prior window state, and attempts to focus it (for keyboard input/navigation) if possible
    /// </summary>
    /// <param name="element"></param>
    public void FocusElement(AutomationElement element)
    {
        try
        {
            var wasSetToForeground = TrySettingElementWindowToForeground(element);
            // fallback to default focusing method
            if (!wasSetToForeground)
            {
                element.SetFocus();
            }
        }
        catch (InvalidOperationException)
        {
            // If the aforementioned method fails, fallback to default focusing method.
            try
            {
                element.SetFocus();
            }
            catch (ElementNotAvailableException)
            {
                // fallback focus method failed as well, no recourse left
                return;
            }
        }
        finally
        {
            // Tentative: can remove to see if it is needed or not.
            Thread.Sleep(100);
        }
    }
    /// <summary>
    /// Sends text input to a valid text book Edit control type.
    /// Focuses the input text box, then enters the text and submits it
    /// </summary>
    /// <param name="inputEl"></param>
    /// <param name="input"></param>
    /// <returns>bool - whether operation was succesful</returns>
    public bool SendTextInput(AutomationElement inputEl, string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }
        if (inputEl == null || (inputEl.Current.ControlType != ControlType.Edit && inputEl.Current.ControlType != ControlType.Document))
        {
            return false;
        }
        // cannot send text to disabled control
        if (!inputEl.Current.IsEnabled)
        {
            return false;
        }

        try
        {
            if (inputEl.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
            {
                var valuePattern = (ValuePattern)pattern;
                if (inputEl.Current.IsKeyboardFocusable)
                {
                    inputEl.SetFocus();
                }
                valuePattern.SetValue(input);
                return true;
            }
            else
            {
                // alternate fallback method to send inputs.
                // Source: https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/add-content-to-a-text-box-using-ui-automation
                try
                {
                    if (inputEl.Current.IsKeyboardFocusable)
                    {
                        inputEl.SetFocus();
                    }
                    var inputWrapper = new InputWrapper();
                    Thread.Sleep(100);
                    inputWrapper.ClearAllText();
                    inputWrapper.SendText(input);
                }
                catch (Exception)
                {
                    // Well, if something still goes wrong, just do nothing I guess ¯\_(ツ)_/¯
                    return false;
                }
            }
            return false;
        }
        catch (InvalidOperationException)
        {
            // object doesn't support the ValuePattern control pattern or some other error occurred
            return false;
        }

    }
    /// <summary>
    /// Finds a Document control type (that contains an Edit control) with a specific ID within the descendants of an element.
    /// If no Document control is found, searchs for an Edit control instead.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="id"></param>
    /// <returns>AutomationElement - Document / Edit control or null</returns>
    public AutomationElement? FindTextInputElement(AutomationElement parent, string? controlName = null, string? controlClassName = null)
    {
        var docCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
        // Find a Document element
        var docElement = parent.FindFirst(TreeScope.Descendants, docCondition);
        if (docElement != null)
        {
            return docElement;
        }
        // No Document element was found so try to look for an Edit element directly
        List<Condition> conditions = new List<Condition>();
        if (!string.IsNullOrEmpty(controlName))
        {
            conditions.Add(new PropertyCondition(AutomationElement.NameProperty, controlName));
        }

        if (!string.IsNullOrEmpty(controlClassName))
        {
            conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, controlClassName));
        }

        conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));

        var editCondition = new AndCondition(conditions.ToArray());
        var editElement = parent.FindFirst(TreeScope.Descendants, editCondition);
        return editElement;
    }
    /// <summary>
    /// Find the AutomationElement for a Window control element
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="windowName"></param>
    /// <param name="windowClass"></param
    /// <returns>AutomationElement - Window control element or null</returns>
    public AutomationElement? FindOpenedWindow(AutomationElement parent, string? windowName = null, string? windowClass = null, ControlType? controlType = null)
    {
        List<Condition> conditions = new List<Condition>();
        if (!string.IsNullOrEmpty(windowName))
        {
            conditions.Add(new PropertyCondition(AutomationElement.NameProperty, windowName));
        }

        if (!string.IsNullOrEmpty(windowClass))
        {
            conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, windowClass));
        }

        if (controlType != null)
        {
            conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
        }

        return parent.FindFirst(TreeScope.Children, new AndCondition(conditions.ToArray()));     //NOTE: If we use Descendants instead of Children here, then the Fetch client operation takes time if there are mulitple windows already opened. THis is because it tries to loop through each element of the already opened windows.
    }

    public AutomationElement? FindElementByAutomationId(AutomationElement parent, string id)
    {
        Condition conditions = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
        return parent.FindFirst(TreeScope.Descendants, conditions);
    }

    /// <summary>
    /// Find the AutomationElement for a Window control element by matching window class and name
    /// </summary>
    /// <param name="windowClass"></param>
    /// <param name="windowName"></param
    /// <returns>AutomationElement - Window control element or null</returns>
    public AutomationElement? FindOpenedWindow(string windowClass, string windowName)
    {
        IntPtr winPtr = Windows32API.FindWindow(windowClass, windowName);
        AutomationElement win32WindowElement = winPtr != IntPtr.Zero ? AutomationElement.FromHandle(winPtr) : null;
        if (win32WindowElement != null)
        {
            return win32WindowElement;
        }
        return null;
    }

    /// <summary>
    /// Brings the window to foreground and restores it to its default state if it's minimized.
    /// </summary>
    /// <param name="mainProcessHandle">Main process handle of the process</param>
    /// <returns></returns>
    public bool SetWindowToForeground(IntPtr mainProcessHandle)
    {
        if (mainProcessHandle == IntPtr.Zero)
        {
            return false;
        }
        //bring AviMark window to foreground
        Windows32API.SetForegroundWindow(mainProcessHandle);
        Windows32API.BringWindowToTop(mainProcessHandle);

        WindowsAutomation automation = new WindowsAutomation();
        //Get main process handle
        var windowElement = automation.GetWindowElementFromHandle(mainProcessHandle);
        if (windowElement == null)
        {
            return false;
        }
        var windowPattern = automation.GetWindowPattern(windowElement);
        if (windowPattern == null)
        {
            return false;
        }
        var current = windowPattern.Current;
        if (current.WindowInteractionState == WindowInteractionState.ReadyForUserInteraction
        && !current.IsModal)
        {
            if (current.WindowVisualState == WindowVisualState.Minimized)
            {
                windowPattern.SetWindowVisualState(WindowVisualState.Normal);
            }
        }
        return true;
    }
    /// <summary>
    /// Submits a text input field by sending an ENTER (RETURN) key press
    /// </summary>
    public void SubmitTextInput()
    {
        var inputWrapper = new InputWrapper();
        inputWrapper.SendEnterKey();
    }
    public void SendEscapeKey()
    {
        var inputWrapper = new InputWrapper();
        inputWrapper.SendEscapeKey();
    }
    public void SendVirtualKey(VirtualKeyCode keyCode)
    {
        var inputWrapper = new InputWrapper();
        inputWrapper.SendVirtualKey(keyCode);
    }
}