using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

class TeamsThumbsUp
{
    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    const uint MOD_CONTROL = 0x0002;
    const uint MOD_SHIFT = 0x0004;
    const uint WM_HOTKEY = 0x0312;
    const uint PM_REMOVE = 0x0001;
    const int HOTKEY_ID = 1;

    const string REACT_BUTTON_NAME = "Reagera";
    const string THUMBSUP_ELEMENT_NAME = "Gilla";

    // Reagera is always in the tree when a message is hovered — cache it
    // Gilla only appears after the picker opens — find it fresh each time
    static UIA3Automation? _automation;
    static AutomationElement? _teamsWindow;
    static AutomationElement? _reactButton;

    static void Main()
    {
        Console.WriteLine("[DEBUG] Registering hotkey F8...");
        if (!RegisterHotKey(IntPtr.Zero, HOTKEY_ID, 0, (uint)Keys.F8)) /* if (!RegisterHotKey(IntPtr.Zero, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Keys.U))*/
        {
            Console.WriteLine("[ERROR] Failed to register hotkey — may already be in use.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("[DEBUG] Hotkey registered.");

        Console.WriteLine("[DEBUG] Caching Teams UI elements...");
        if (!CacheElements())
        {
            Console.WriteLine("[ERROR] Could not find Teams window. Make sure Teams is open.");
            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            return;
        }
        Console.WriteLine("[DEBUG] Ready — hover over a Teams message and press Ctrl+Shift+U.");
        Console.WriteLine("Press Enter to quit.");

        bool running = true;
        System.Threading.Thread quitThread = new System.Threading.Thread(() =>
        {
            Console.ReadLine();
            running = false;
        });
        quitThread.IsBackground = true;
        quitThread.Start();

        while (running)
        {
            if (PeekMessage(out MSG msg, IntPtr.Zero, WM_HOTKEY, WM_HOTKEY, PM_REMOVE))
            {
                if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
                    SendThumbsUp();
            }
            System.Threading.Thread.Sleep(50);
        }

        Console.WriteLine("[DEBUG] Exiting.");
        _automation?.Dispose();
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
    }

    static bool CacheElements()
    {
        try
        {
            _automation = new UIA3Automation();
            var desktop = _automation.GetDesktop();

            _teamsWindow = desktop.FindAllChildren()
                .FirstOrDefault(w => w.Name.Contains("Microsoft Teams"));

            if (_teamsWindow == null)
            {
                Console.WriteLine("[ERROR] Could not find a Teams window.");
                return false;
            }
            Console.WriteLine($"[DEBUG] Found Teams window: \"{_teamsWindow.Name}\"");

            // Reagera may not be visible yet (no message hovered) — that's ok,
            // we'll try to find it on each keypress if it's null here
            var cf = _automation.ConditionFactory;
            _reactButton = _teamsWindow.FindFirst(
                FlaUI.Core.Definitions.TreeScope.Descendants,
                cf.ByName(REACT_BUTTON_NAME)
            );
            Console.WriteLine($"[DEBUG] Reagera cached: {_reactButton != null}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CacheElements failed: {ex.Message}");
            return false;
        }
    }

    static void SendThumbsUp()
    {
        try
        {
            var cf = _automation!.ConditionFactory;

            // Re-find Reagera if cache is stale (e.g. Teams reloaded)
            if (_reactButton == null)
            {
                _reactButton = _teamsWindow!.FindFirst(
                    FlaUI.Core.Definitions.TreeScope.Descendants,
                    cf.ByName(REACT_BUTTON_NAME)
                );
            }

            if (_reactButton == null)
            {
                Console.WriteLine("[ERROR] Could not find Reagera — hover over a message first.");
                return;
            }

            // Open the reaction picker
            _reactButton.Patterns.ExpandCollapse.Pattern.Expand();

            // Poll for Gilla — appears quickly once picker is open
            AutomationElement? thumbsUp = null;
            int waited = 0;
            while (thumbsUp == null && waited < 2000)
            {
                thumbsUp = _teamsWindow!.FindFirst(
                    FlaUI.Core.Definitions.TreeScope.Descendants,
                    cf.ByName(THUMBSUP_ELEMENT_NAME)
                );
                if (thumbsUp == null)
                {
                    System.Threading.Thread.Sleep(50);
                    waited += 50;
                }
            }

            if (thumbsUp == null)
            {
                Console.WriteLine("[ERROR] Picker opened but Gilla not found.");
                return;
            }

            thumbsUp.Patterns.Invoke.Pattern.Invoke();
            Console.WriteLine($"[DEBUG] Thumbs up sent (waited {waited}ms for picker).");

            // Invalidate Reagera cache — it may move when message changes
            _reactButton = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            Console.WriteLine("[ERROR] Restarting cache...");
            CacheElements();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public System.Drawing.Point pt;
    }
}