# KeyRoute

A lightweight Windows background utility that reclaims underused keyboard keys and dedicates them to a single application — without those keys triggering their normal function anywhere else on the system.

---

## The Problem

Some applications (Discord, OBS, streaming and voice tools) support **global hotkeys** — shortcuts that fire even when the app isn't focused. That's useful, but it creates a conflict: the key you bind still performs its *original* job everywhere else.

Bind `Home` to mute in Discord, and every time you mute, your text cursor also jumps to the start of the line in whatever you were typing. The key does two things at once, and you only wanted one.

Disabling the key outright doesn't work either. A low-level keyboard hook sits *earlier* in the input chain than an application's global hotkey listener — so if you block the key, the target application stops receiving it too. You lose the shortcut along with the side effect.

## The Solution

KeyRoute intercepts the chosen key, **blocks the original keystroke**, and **injects a substitute key** in its place.

The substitute keys are **F13 through F24** — key codes that Windows fully recognises but that no physical keyboard produces. Because no hardware can generate them, no application has ever bound a shortcut to them. They are effectively a private, collision-free signalling channel.

```
You press:        Home
Hook blocks:      Home  ──►  never reaches any application
Hook injects:     F14   ──►  reaches Discord only
Result:           Discord responds. Nothing else does.
```

From the user's perspective it is still **one physical keypress**. The substitution is invisible.

## Capacity

The F13–F24 range provides **12 available substitution slots**, meaning up to **12 physical keys** can be reclaimed and dedicated to target applications simultaneously.

| Substitute | Key code |
|---|---|
| F13 | 124 |
| F14 | 125 |
| F15 | 126 |
| F16 | 127 |
| F17 | 128 |
| F18 | 129 |
| F19 | 130 |
| F20 | 131 |
| F21 | 132 |
| F22 | 133 |
| F23 | 134 |
| F24 | 135 |

Any key that is rarely used in normal work — `Insert`, `Home`, `End`, `Pause`, `Scroll Lock`, `Page Up/Down`, the numpad block — is a good candidate for reclaiming.

Mappings aren't fixed to any order — pick any physical key and assign it to whichever F13–F24 slot you want, from the app itself.

---

## How It Works

| Component | Purpose |
|---|---|
| `SetWindowsHookEx` (`user32.dll`) | Installs a `WH_KEYBOARD_LL` low-level hook that receives every keystroke system-wide before any application sees it. |
| Callback delegate | The function Windows invokes on each key event. Inspects the key and decides whether to block or pass it through. |
| `keybd_event` (`user32.dll`) | Injects the substitute keystroke (a key-down event followed by a key-up event). |
| `CallNextHookEx` (`user32.dll`) | Passes unhandled keys down the hook chain so the rest of the keyboard behaves normally. |
| Return value `1` | Consumes the keystroke, preventing it from reaching any application. |
| `config.json` (next to the exe) | Stores your mappings between runs. Created automatically the first time you add one. |

Only the configured keys are intercepted. Every other key is forwarded untouched.

## Using the app

Run the exe and a small dark window opens with your current mappings.

- **Add mapping** — click it, then press the physical key you want to reclaim. A dropdown of the still-unused F13–F24 keys appears; pick one and confirm.
- **Double-click a row** to reassign it to a different F13–F24 key at any time.
- **Remove selected** — deletes a mapping and immediately stops intercepting that key.

Every change takes effect immediately and is saved to `config.json`, so it's still there next time you launch. Up to 12 mappings can exist at once (one per F13–F24 slot).

---

## Setup

### 1. Build and publish

Standalone single-file executable (bundles the .NET runtime, runs on any Windows machine):

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o .\publish
```

Smaller output, requires .NET installed on the target machine:

```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -o .\publish
```

Publish to a custom location:

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o "D:\Tools\KeyRoute"
```

### 2. Add a mapping and configure the target application

Run KeyRoute, click **Add mapping**, press the physical key you want to reclaim, and pick which F13–F24 key it should send.

Then, in the target application's hotkey settings, bind the desired action — while it's listening for a keybind, press your *physical* key. It will record the substitute KeyRoute is now sending in its place.

### 3. Run at startup

```
Win + R  →  shell:startup
```

Place a shortcut to the executable in the folder that opens. It will launch automatically on login.

Keep the executable in a permanent location outside the build directory — the shortcut references a fixed path and will break silently if the file moves.

---

## Development Commands

| Command | Purpose |
|---|---|
| `dotnet build` | Compile |
| `dotnet run` | Compile and run (console output visible during development) |
| `dotnet clean` | Remove build artifacts |
| `dotnet restore` | Restore dependencies |

### Project configuration

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
<AssemblyName>KeyRoute</AssemblyName>
```

- `WinExe` suppresses the console window for background operation. Use `Exe` during development to see console output.
- `-windows` on the target framework is required for Windows Forms.
- `UseWindowsForms` provides `Application.Run()` (the message loop the hook depends on) and the `Keys` enumeration.

---

## Notes and Limitations

**A message loop is required.** A low-level keyboard hook is delivered by message to the installing thread. Without an active message loop, Windows waits on every keystroke until it times out — producing severe system-wide input lag. `Application.Run()` provides the loop.

**The callback delegate must be kept alive.** If the delegate passed to `SetWindowsHookEx` is garbage collected while the hook is active, the hook fails silently. Store it in a static field for the lifetime of the process.

**Antivirus software may flag the executable.** Installing a global keyboard hook and injecting keystrokes are the same techniques used by keyloggers. The warning reflects the method, not the behaviour of this tool. An exclusion may be required.

**No tray icon yet.** Closing the window exits the app and removes the hook. Keep it open (or minimized) while you want your mappings active.

---

## Planned

- System tray icon (`NotifyIcon`) with exit and toggle controls
- Optional per-application filtering via `GetForegroundWindow`
- In-app "run at startup" toggle using the registry `Run` key
