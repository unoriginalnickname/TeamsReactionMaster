using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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

    const uint WM_HOTKEY = 0x0312;
    const uint PM_REMOVE = 0x0001;
    const int HOTKEY_ID = 1;

    // All known translations — app scans these at startup to find which one Teams is using
    static readonly string[] ReactButtonNames = {
        "React",        // English
        "Reagera",      // Swedish
        "Reagieren",    // German
        "Réagir",       // French
        "Reaccionar",   // Spanish
        "Reageren",     // Dutch
        "Reagir",       // Portuguese
        "Reagisci",     // Italian
        "Zareaguj",     // Polish
        "Reagoi",       // Finnish
        "Reager",       // Norwegian / Danish
    };

    static readonly string[] ThumbsUpButtonNames = {
        "Like",         // English
        "Gilla",        // Swedish
        "Gefällt mir",  // German
        "J'aime",       // French
        "Me gusta",     // Spanish
        "Vind ik leuk", // Dutch
        "Curtir",       // Portuguese
        "Mi piace",     // Italian
        "Lubię to",     // Polish
        "Tykkää",       // Finnish
        "Liker",        // Norwegian
        "Synes godt om",// Danish
    };

    // Cached elements — set at startup, invalidated after each use
    static UIA3Automation? _automation;
    static AutomationElement? _teamsWindow;
    static AutomationElement? _reactButton;
    static string _reactName = "Reagera";
    static string _thumbsUpName = "Gilla";

    static void Main()
    {
        Console.WriteLine("[DEBUG] Registering hotkey F8...");
        if (!RegisterHotKey(IntPtr.Zero, HOTKEY_ID, 0, (uint)Keys.F8))
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

        Console.WriteLine($"[DEBUG] Ready — using react button \"{_reactName}\", thumbs up \"{_thumbsUpName}\".");
        Console.WriteLine("Hover over a Teams message and press F8.");
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
            var cf = _automation.ConditionFactory;

            _teamsWindow = desktop.FindAllChildren()
                .FirstOrDefault(w => w.Name.Contains("Microsoft Teams"));

            if (_teamsWindow == null)
            {
                Console.WriteLine("[ERROR] Could not find a Teams window.");
                return false;
            }
            Console.WriteLine($"[DEBUG] Found Teams window: \"{_teamsWindow.Name}\"");

            // Scan react button translations — store whichever one Teams is using
            _reactButton = null;
            foreach (var name in ReactButtonNames)
            {
                var found = _teamsWindow.FindFirst(
                    FlaUI.Core.Definitions.TreeScope.Descendants,
                    cf.ByName(name)
                );
                if (found != null)
                {
                    _reactButton = found;
                    _reactName = name;
                    Console.WriteLine($"[DEBUG] React button found with name: \"{name}\"");
                    break;
                }
            }

            // Scan thumbs up translations — determine which language Teams is in
            // We can't find Gilla/Like yet (picker not open) but we set the name
            // based on whichever language the react button matched
            int reactIndex = Array.IndexOf(ReactButtonNames, _reactName);
            if (reactIndex >= 0 && reactIndex < ThumbsUpButtonNames.Length)
            {
                _thumbsUpName = ThumbsUpButtonNames[reactIndex];
                Console.WriteLine($"[DEBUG] Thumbs up will look for: \"{_thumbsUpName}\"");
            }

            Console.WriteLine($"[DEBUG] React button cached: {_reactButton != null}");
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

            // Re-find react button if cache is stale
            if (_reactButton == null)
            {
                _reactButton = _teamsWindow!.FindFirst(
                    FlaUI.Core.Definitions.TreeScope.Descendants,
                    cf.ByName(_reactName)
                );
            }
            if (_teamsWindow == null)
            {
                Console.WriteLine("[ERROR] Microsoft Teams window not found. Make sure Teams is open.");
                return;
            }

            if (_reactButton == null)
            {
                Console.WriteLine("[ERROR] React button not found.");
                Console.WriteLine("Possible causes:");
                Console.WriteLine("- No message is currently hovered");
                Console.WriteLine("- Teams UI has not loaded fully");
                Console.WriteLine("- UI layout changed or language mismatch");
                return;
            }

            // Open the reaction picker
            _reactButton.Patterns.ExpandCollapse.Pattern.Expand();

            // Poll for thumbs up — appears once picker is open
            AutomationElement? thumbsUp = null;
            int waited = 0;
            while (thumbsUp == null && waited < 2000)
            {
                thumbsUp = _teamsWindow!.FindFirst(
                    FlaUI.Core.Definitions.TreeScope.Descendants,
                    cf.ByName(_thumbsUpName)
                );
                if (thumbsUp == null)
                {
                    System.Threading.Thread.Sleep(50);
                    waited += 50;
                }
            }

            if (thumbsUp == null)
            {
                Console.WriteLine($"[ERROR] Picker opened but \"{_thumbsUpName}\" not found.");
                return;
            }

            thumbsUp.Patterns.Invoke.Pattern.Invoke();
            Console.WriteLine($"[DEBUG] Thumbs up sent (waited {waited}ms for picker).");

            // Invalidate react button cache — changes when message focus changes
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