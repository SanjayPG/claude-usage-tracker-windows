# Claude Usage Tracker for Windows

A small Windows tray app that shows how much of your Claude **5-hour session** and **weekly** usage you have used, with reset times.

It is a standalone app. It is not part of the Claude desktop app, and it never reads your Claude login. It reads a small file written by the [usage-tracker plugin for Claude Code](https://github.com/SanjayPG/claude-code-usage-tracker).

- **Tray icon:** two stacked bars (session on top, week below), green under 50% used, amber from 50 to 80%, red above 80%.
- **Hover:** a tooltip with both numbers and reset times.
- **Left-click:** a dark card with the bars, percentages, reset times and "updated X min ago".
- **Right-click menu:** Reload feed, Refresh from Claude Code, Auto-refresh every 30 min, Start with Windows, Open feed folder, Exit.
- **Alerts:** a notification once at 80% and once at 90% per window.
- **After a reset:** shows "reset / waiting for data" instead of an old percentage.

## How it works

```
Claude Code ──(usage-tracker plugin)──> ~/.claude/usage-feed.json ──> this tray app
```

Claude Code reports your usage with every reply. The plugin writes the percentages and reset times to `%USERPROFILE%\.claude\usage-feed.json`, and the tray app watches that file.

**Limit:** the file only changes while Claude Code is running, and only when your usage moves by a whole percentage point. If you have only been using the claude.ai website or the desktop chat, use **Refresh from Claude Code** in the menu. It runs one very short request through the official `claude` CLI, which updates the file. It uses a small amount of your quota each time, so it is manual by default. **Auto-refresh** is off until you tick it, and it skips a refresh if the file was updated in the last 10 minutes.

## Requirements

- Windows. Built and tested on Windows 11 only; Windows 10 is untested.
- The [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (the SDK includes it).
- [Claude Code](https://claude.com/claude-code) on a Pro or Max plan, with the plugin installed:

```
/plugin install usage-tracker --marketplace SanjayPG/claude-code-usage-tracker
```

Plugin version 0.3.0 or later is needed, because that is the version that writes the feed file. Restart Claude Code once after installing or updating.

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
- **No login or token access.** It never reads Claude's credentials file or any cookie. The only external program it starts is `claude.exe` (for Refresh) and `explorer.exe` (for Open feed folder).
- **What it touches:** it reads `~/.claude/usage-feed.json`, writes its own settings to `%APPDATA%\ClaudeUsageTray\settings.json`, and, only if you tick Start with Windows, adds one value to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Build it yourself** instead of trusting a downloaded exe, and run `dotnet test`.
- **Watch it.** Windows Firewall or Sysinternals Process Monitor will show it makes no outbound connections.

Honest limits:

- A downloaded exe is **unsigned**, so Windows SmartScreen will warn. Compare the SHA-256 of any release file with the one published, or build from source.
- This has not had an independent security review.
- **Refresh from Claude Code** starts the normal `claude` CLI, which loads your usual plugins and hooks, so those run too.
- Any program on your PC can read the feed file. It holds only percentages and reset times.

## Tests

`dotnet test` runs 60 unit tests covering feed parsing, colour thresholds, reset wording, "updated X ago", alerts, settings and the auto-refresh rule. The tray icon and popup drawing are checked by eye with `--preview`, not by automated tests.

## Licence

MIT. See [LICENSE](LICENSE).
