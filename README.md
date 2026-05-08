# EmojiMaster for Teams

A lightweight desktop utility for instantly sending 👍 reactions in Microsoft Teams using a global hotkey.

Press **F8** while hovering over a Teams message, and the app automatically opens the reaction picker and clicks **Thumbs Up / Like**, even across different Teams language localizations.

Built in C# using Windows UI Automation via FlaUI.

---

## Features

- Global **F8 hotkey**
- Automatically detects Teams UI language
- Supports multiple localizations:

| Language | React | Like |
|----------|-------|------|
| English | React | Like |
| Swedish | Reagera | Gilla |
| German | Reagieren | Gefällt mir |
| French | Réagir | J'aime |
| Spanish | Reaccionar | Me gusta |
| Dutch | Reageren | Vind ik leuk |
| Portuguese | Reagir | Curtir |
| Italian | Reagisci | Mi piace |
| Polish | Zareaguj | Lubię to |
| Finnish | Reagoi | Tykkää |
| Norwegian | Reager | Liker |
| Danish | Reager | Synes godt om |

- Auto-recovers if Teams UI changes
- Lightweight console-based utility
- No Teams plugins required

---

## How It Works

The program:

1. Registers **F8** as a global Windows hotkey
2. Finds the active Microsoft Teams window
3. Detects the localized **React** button text
4. Maps it to the matching localized **Thumbs Up** text
5. Waits for you to hover over a Teams message
6. Press **F8**
7. Opens the reaction picker
8. Clicks 👍 automatically

---

## Requirements

- Windows
- .NET
- Microsoft Teams (desktop app)
- UI Automation enabled (default on Windows)

NuGet dependency:

```bash
FlaUI.UIA3
```

Install:

```bash
dotnet add package FlaUI.UIA3
```

---

## Run

Build and run:

```bash
dotnet run
```

You should see:

```text
[DEBUG] Registering hotkey F8...
[DEBUG] Hotkey registered.
[DEBUG] Caching Teams UI elements...
[DEBUG] Ready...
Hover over a Teams message and press F8.
```

---

## Usage

### Send thumbs up

- Open Microsoft Teams
- Hover over a message
- Press **F8**

The reaction is sent instantly.

---

### Quit

Press:

```text
Enter
```

in the console window

---

## Troubleshooting

### Could not find Teams window

Make sure Teams is running and visible.

---

### Failed to register hotkey

Another application is already using **F8**.

Change:

```csharp
Keys.F8
```

to another key.

---

### Hover over a message first

The app can only locate reaction controls when your cursor is over a Teams message.

---

### Localization mismatch

If your Teams language is unsupported, add translations to:

```csharp
ReactButtonNames
ThumbsUpButtonNames
```

---

## Technical Notes

Uses:

- Win32 hotkey registration
- Windows message polling
- Microsoft UI Automation
- FlaUI UIA3 backend
- Dynamic cache invalidation for Teams focus changes

---

## Limitations

- Depends on Teams UI structure staying reasonably stable
- Requires the Teams desktop app
- May break after major Teams UI updates

---

## Why This Exists

Reacting in Teams takes too many clicks.

This makes sending 👍 essentially instant.

---

## License

MIT
