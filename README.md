# Claude Usage Tracker for Windows

A small Windows tray app that shows how much of your Claude **5-hour session** and **weekly** usage you have used, with reset times.

It is a standalone app. It is not part of the Claude desktop app, and it never reads your Claude login or any token. It gets your usage from the official **Claude Code** command line, which you need installed and signed in. A Claude Code plugin is optional, for live updates.

- **Tray icon:** two stacked bars (session on top, week below), green under 50% used, amber from 50 to 80%, red above 80%.
- **Hover:** a tooltip with both numbers and reset times.
- **Left-click:** a dark card with the bars, percentages, reset times and "updated X min ago".
- **Right-click menu:** Reload feed, Refresh from Claude Code, Auto-refresh every 30 min, Start with Windows, Open feed folder, Exit.
- **Alerts:** a notification once at 80% and once at 90% per window.
- **After a reset:** shows "reset / waiting for data" instead of an old percentage.

## How it gets your numbers

There are two sources, and the app shows whichever was updated most recently.

**1. Refresh from Claude Code (works on its own, no plugin).** The menu item runs one very short request through the official `claude` command (`claude -p --output-format stream-json --verbose`). Claude Code prints your usage in that output, and the app reads it and keeps its own copy in `%APPDATA%\ClaudeUsageTray\last-refresh.json`. It works even if you have only used the claude.ai website or the desktop chat, because your usage is one shared pool. Each refresh uses a small amount of your quota, so it is manual by default. **Auto-refresh every 30 min** is off until you tick it, and it skips a refresh if the numbers were updated in the last 10 minutes.

**2. The optional plugin (live while you use Claude Code).** With the [usage-tracker plugin](https://github.com/SanjayPG/claude-code-usage-tracker) (version 0.3.0 or later), Claude Code also writes your usage to `%USERPROFILE%\.claude\usage-feed.json` as you work, and the tray app watches that file. Without the plugin, the numbers only change when you refresh.

```
claude -p  (Refresh menu item) ─────────────────────> last-refresh.json ──┐
Claude Code + usage-tracker plugin (optional) ──────> usage-feed.json ────┴──> tray app (newest wins)
```

**Limit:** the app reads Claude Code's output, a format Anthropic does not document as stable, so a future Claude Code update could break the refresh. If that happens the app keeps showing the last numbers and says it could not refresh.

## Requirements

- Windows. Built and tested on Windows 11 only; Windows 10 is untested.
- The [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (the SDK includes it).
- [Claude Code](https://claude.com/claude-code) installed and signed in, on a Pro or Max plan (usage figures are only reported for those plans).
- Optional, for live updates, the plugin. At the Claude Code prompt:

```
/plugin install usage-tracker --marketplace SanjayPG/claude-code-usage-tracker
```

Restart Claude Code once after installing the plugin.

## Build and run

```
dotnet test
dotnet publish src/ClaudeUsageTray -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
publish\ClaudeUsageTray.exe
```

On Windows 11, drag the icon out of the hidden-icons arrow onto the taskbar to keep it visible. To start it with Windows, tick **Start with Windows** in the menu, after copying the exe to a permanent folder (autostart remembers the exe's location).

Developer switches:

- `ClaudeUsageTray.exe --preview <folder>` writes PNGs of the popup and icon from sample data.
- `ClaudeUsageTray.exe --refresh-once` runs the Claude Code refresh once and exits (0 = ok).

## Is it safe? How to check

The app is about 1,000 lines of C# and does very little. You do not need to take this on trust:

- **No network code.** Search the source for `HttpClient`, `Socket` and `WebClient`. There are no matches.
- **No login or token access.** It never reads Claude's credentials file or any cookie. The only external programs it starts are `claude.exe` (for Refresh) and `explorer.exe` (for Open feed folder).
- **What it touches:** it reads the plugin feed `~/.claude/usage-feed.json` if present, writes its own settings and last reading to `%APPDATA%\ClaudeUsageTray\`, and, only if you tick Start with Windows, adds one value to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Build it yourself** instead of trusting a downloaded exe, and run `dotnet test`.
- **Watch it.** Windows Firewall or Sysinternals Process Monitor will show it makes no outbound connections of its own. (The `claude` command it starts talks to Anthropic, as it always does.)

Honest limits:

- A downloaded exe is **unsigned**, so Windows SmartScreen will warn. Compare the SHA-256 of any release file with the one published, or build from source.
- This has not had an independent security review.
- **Refresh from Claude Code** starts the normal `claude` CLI, which loads your usual plugins and hooks, so those run too.
- Any program on your PC can read the files above. They hold only percentages and reset times.

## Tests

`dotnet test` runs 79 unit tests covering feed and Claude Code output parsing, colour thresholds, reset wording, "updated X ago", alerts, settings and the auto-refresh rule. The tray icon and popup drawing are checked by eye with `--preview`, not by automated tests.

## Licence

MIT. See [LICENSE](LICENSE).
